using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Pingr.Application.Abstractions;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Email;
using Pingr.Domain.Entities;
using Monitor = Pingr.Domain.Entities.Monitor;

namespace Pingr.Infrastructure.Services.Monitors;

/// <inheritdoc/>
public sealed class MonitorService(
    HttpClient httpClient,
    IWorkspaceService workspaceService,
    IUnitOfWork unitOfWork,
    IChatService chatService,
    IMonitorRepository monitorRepository) : IMonitorService
{
    public async Task<MonitorCheckResult> ExecuteCheckAsync(
        Monitor monitor,
        CancellationToken cancellationToken = default)
    {
        // Build message
        using var httpRequestMessage = new HttpRequestMessage(
            new HttpMethod(monitor.HttpMethod),
            monitor.Url);

        if (monitor.Body is not null)
        {
            httpRequestMessage.Content = new StringContent(
                monitor.Body,
                Encoding.UTF8);
        }

        foreach (var httpHeader in monitor.HttpHeaders)
        {
            httpRequestMessage.Headers.TryAddWithoutValidation(
                httpHeader.Key,
                httpHeader.Value);
        }

        httpRequestMessage.Headers.CacheControl = new CacheControlHeaderValue
        {
            NoStore = true,
            NoCache = true
        };

        MonitorStatus status = MonitorStatus.Down;
        int? statusCode = null; 
        MonitorFailureReason? failureReason = null;
        string? message = null;

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(monitor.TimeoutSeconds));
            
            using var responseMessage = await httpClient.SendAsync(
                httpRequestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);

            // Check if status code is expected
            if (monitor.ExpectedStatusCodes.Contains((int)responseMessage.StatusCode))
            {
                status = MonitorStatus.Healthy;
                statusCode = (int) responseMessage.StatusCode;
                monitor.UpdateLastSuccessfulCheckAt();
            }
            else
            {
                failureReason = MonitorFailureReason.UnexpectedStatusCode;
                statusCode = (int) responseMessage.StatusCode;
                message = responseMessage.ReasonPhrase;
            }
        }
        // If the request exceeded its timeout limit
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            failureReason = MonitorFailureReason.Timeout;
            message = "The request exceeded the configured timeout.";
        }
        // If the request failed
        catch (HttpRequestException httpRequestException)
        {
            failureReason = MonitorFailureReason.RequestFailed;
            
            if (httpRequestException.StatusCode is not null)
            {
                statusCode = (int) httpRequestException.StatusCode;
            }
            
            message = httpRequestException.HttpRequestError.ToString();
        }
        finally
        {
            stopwatch.Stop();
            
            monitor.UpdateLastCheckedAt();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        
        return new MonitorCheckResult(
            status,
            statusCode,
            stopwatch.Elapsed,
            failureReason, 
            message,
            DateTimeOffset.UtcNow);
    }

    public async Task HandleStatusTransitionAsync(
        Monitor monitor,
        MonitorCheckResult checkResult,
        CancellationToken cancellationToken = default)
    {
        var previousCheckResult = monitor.LastCheckResult;

        if (checkResult.CheckedAt < previousCheckResult?.CheckedAt)
            return;
        
        await monitorRepository.UpdateLastCheckResultAsync(
            monitor,
            checkResult,
            cancellationToken);

        if (previousCheckResult is null)
            return;
        
        if ((checkResult.Status == MonitorStatus.Down && previousCheckResult?.Status != MonitorStatus.Down)
            || (checkResult.Status == MonitorStatus.Healthy  && previousCheckResult?.Status != MonitorStatus.Healthy))
        {
            var emailMessageDetails = await BuildMonitorStatusEmailAsync(
                monitor,
                checkResult,
                cancellationToken);
                
            await workspaceService.SendEmailToEveryMemberAsync(
                monitor.WorkspaceId,
                emailMessageDetails, 
                cancellationToken);
        }
    }

    private async Task<EmailMessageDetails> BuildMonitorStatusEmailAsync(
        Monitor monitor,
        MonitorCheckResult checkResult,
        CancellationToken cancellationToken = default)
    {
        var emailSubject = 
            (checkResult.Status == MonitorStatus.Healthy
            ? MonitorEmailTemplates.MonitorRecovered(monitor.Name)
            : MonitorEmailTemplates.MonitorDown(monitor.Name))
            .Subject;
        
        var jsonStatus = JsonSerializer.Serialize(checkResult);
        var prompt = MonitorStatusEmailPrompt.Build(jsonStatus);
        
        var chatResponse = await chatService.SendAsync(
            prompt,
            cancellationToken);

        return new EmailMessageDetails(
            null,
            null,
            emailSubject,
           chatResponse.Text);
    }
}

public static class MonitorStatusEmailPrompt
{
    public static string Build(string jsonCheckResult) =>
        $"""
         You are generating a monitor status email for software engineering team.
         If the monitor is healthy then do not include the failure reason, status code or message.
         
         Requirements:
         - Output valid Markdown only.
         - Do not invent information.
         - Use concise professional language only.
         - No emojis allowed.
         - Do not include a greeting or email subject.
         
         Status:
         {jsonCheckResult}
         """;
}