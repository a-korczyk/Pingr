using System.Security.Cryptography;
using System.Text;
using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Email;
using Pingr.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Pingr.Application.Abstractions.Services.Logs;

namespace Pingr.Infrastructure.Services.Logs;

/// <inheritdoc/>
public sealed class LogNotificationService(
    IWorkspaceService workspaceService,
    ICacheService cacheService,
    IConfiguration configuration) : ILogNotificationService
{
    public async Task NotifyCriticalErrorAsync(
        Log log,
        User user,
        CancellationToken cancellationToken)
    {
        var maxAmount = Convert.ToInt32(configuration["Email:Limits:CriticalErrors:Max"]); 
        var maxPerMinutes = Convert.ToInt32(configuration["Email:Limits:CriticalErrors:PerMinutes"]);
        
        var criticalErrorSignature = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(log.Title)));
        
        // Check if this critical error has already been sent based on its title
        var wasCriticalErrorEmailed =
            await cacheService.GetAsync<bool>($"log-notifications:critical_error:{log.Status}:{user.Id}:{criticalErrorSignature}");
        
        if (wasCriticalErrorEmailed.Exists)
            return;
        
        // Check if user accepts anymore log emails
        var amountOfLogEmailSent = 
            await cacheService.GetAsync<int>($"email:logs:{user.Id}");
        
        if (amountOfLogEmailSent.Exists && amountOfLogEmailSent.Value >= maxAmount)
            return;

        // Send email
        var emailTemplate = log.Status switch
        {
            LogStatus.Pending => EmailTemplates.CriticalErrorLogged(log),
            LogStatus.Resolved => EmailTemplates.CriticalErrorResolved(log),
            _ => throw new InvalidOperationException("Unsupported critical error status.")
        };

        await workspaceService.SendEmailToEveryMemberAsync(
            log.WorkspaceId,
            new(
                null,
                null,
                emailTemplate.Subject,
                emailTemplate.Body), 
            cancellationToken);
        
        // Update cache
        await cacheService.SetAsync(
            $"log-notifications:critical_error:{log.Status}:{user.Id}:{criticalErrorSignature}",
            true,
            TimeSpan.FromMinutes(maxPerMinutes));
        
        await cacheService.IncrementWithExpirationAsync(
            $"email:logs:{user.Id}",
            TimeSpan.FromMinutes(maxPerMinutes));
    }
}