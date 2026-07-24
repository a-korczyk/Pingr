using System.Text.Json;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Email;
using Microsoft.Extensions.DependencyInjection;
using Pingr.Application.Abstractions.Services.Logs;

namespace Pingr.Infrastructure.Services.Logs.Digest;

/// <summary>
/// Builds a <see cref="EmailMessageDetails"/> for log digest emails.
/// </summary>
public interface ILogDigestEmailBuilder
{
    /// <remarks>
    /// Builds a generic email message with no recipient email set.
    /// </remarks>
    public Task<EmailMessageDetails?> Build(
        KeyValuePair<Guid, IReadOnlyDictionary<Guid, LogDigestEntry>> workspace,
        CancellationToken cancellationToken);
}

/// <inheritdoc/>
public sealed class LogDigestEmailBuilder(
    IServiceScopeFactory serviceScopeFactory,
    ILogDigestStatisticsBuilder statisticsBuilder,
    IChatService chatService) : ILogDigestEmailBuilder
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = 
        new JsonSerializerOptions { WriteIndented = true };
    
    public async Task<EmailMessageDetails?> Build(
        KeyValuePair<Guid, IReadOnlyDictionary<Guid, LogDigestEntry>> workspace,
        CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var workspaceRepository = scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>();
        
        var workspaceEntity = await workspaceRepository.GetByWorkspaceIdAsync(
            workspace.Key,
            cancellationToken);

        if (workspaceEntity == null)
            return null;
        
        try
        {
            // Process statistics
            var statistics = statisticsBuilder.Build(workspace.Value);
            var jsonStatistics = JsonSerializer.Serialize(
                statistics,
                _jsonSerializerOptions);
            
            // Get summary
            var prompt = LogDigestSummaryPrompt.Build(jsonStatistics);
            var chatResponse = await chatService.SendAsync(prompt, cancellationToken);
            var markdownSummary = chatResponse.Text;

            var logDigestEmailTemplate = EmailTemplates.LogDigest(workspaceEntity.Name);
            
            return new EmailMessageDetails(
                null,
                null,
                logDigestEmailTemplate.Subject,
                markdownSummary);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);

            var failedLogDigestEmailTemplate = EmailTemplates.LogDigestFailed(workspaceEntity.Name); 
            
            return new EmailMessageDetails(
                null,
                null,
                failedLogDigestEmailTemplate.Subject,
                failedLogDigestEmailTemplate.Body);
        }
    }
}

/// <summary>
/// Adds the provided digest statistics into the prompt.
/// </summary>
public static class LogDigestSummaryPrompt
{
    /// <param name="jsonStatistics"><see cref="LogDigestStatistics"/> in JSON.</param>
    public static string Build(string jsonStatistics) =>
        $"""
        You are generating a log digest email for a software engineering team.
        
        Requirements:
        - Output valid Markdown only.
        - Do not invent information.
        - Use concise professional language only.
        - No emojis allowed.
        - Do not include a greeting or email subject.
        - Prioritize pending issues.
        - Group related issues together.
        - Explain the operational impact briefly.
        - Highlight the most important issues first.
        - Do not mention anything like "Log summary from the last X hours".
        
        Use:
        - h2 headings
        - ul/li lists
        - strong tags for counts
        - tables
        
        Output structure:
        - Summary (brief overview with a table showing the amount of logs per type)
        - Critical issues (if any, list pending, in progress or resolved critical errors) 
        - Errors requiring attention (if any, list only pending or in progress errors)
        - Common warnings (if any, list only pending or in progress most frequent warnings)
        - General overview (table showing the amount of logs per type per status (read "TypeCountByStatus")
        - Possible impact (explain the operational impact briefly)
        
        Rules:
        - Always show every critical error in "Critical issues" section.
        - Show max 5 unique errors in "Errors requiring attention" section, if there are more then inform at the
        bottom of the section of the amount of additional errors that haven't been shown in the section.
        - Show max 5 unique warnings in "Common warnings" section, if there are more then inform at the
        bottom of the section of the amount of additional warnings that haven't been shown in the section.
        - If a section is empty then don't include it.
        - Critical error = type 3
        - Error = type 2
        - Warning = type 1
        - Pending = status 0
        - InProgress = status 1
        - Resolved = status 2
        
        Statistics:
        {jsonStatistics}
        """;
}