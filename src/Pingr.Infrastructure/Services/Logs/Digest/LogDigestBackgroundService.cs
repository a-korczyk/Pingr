using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pingr.Application.Abstractions.Services.Logs;
using IEmailSender = Pingr.Application.Abstractions.Services.Email.IEmailSender;

namespace Pingr.Infrastructure.Services.Logs.Digest;

/// <summary>
/// Periodically processes log digests by generating and sending
/// summary emails to each user of the workspace.
/// </summary>
public sealed class LogDigestBackgroundService(
    ILogDigestQueue logDigestQueue,
    ILogDigestEmailBuilder logDigestEmailBuilder,
    IServiceScopeFactory serviceScopeFactory,
    IEmailSender emailSender) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var workspaceService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var workspaces = await logDigestQueue.TakeWorkspacesAsync();

            foreach (var workspace in workspaces)
            {
                var genericMessage = await logDigestEmailBuilder.Build(
                    workspace,
                    stoppingToken);
                
                if (genericMessage is null)
                    continue;
                
                await workspaceService.SendEmailToEveryMemberAsync(
                    workspace.Key,
                    genericMessage, 
                    stoppingToken);
            }

            await Task.Delay(TimeSpan.FromMinutes(0.5), stoppingToken); //todo change to 30 minutes!
        }

    }
}