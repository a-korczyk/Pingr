using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pingr.Application.Abstractions.Services;

namespace Pingr.Infrastructure.Services.Monitors;

public sealed class MonitorBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IMonitorQueue monitorQueue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await monitorQueue.InitializeAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var dueMonitors = await monitorQueue.TakeDueMonitorsAsync(stoppingToken);

            await Task.WhenAll(
                dueMonitors.Select(async dueMonitor =>
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    var monitorService = scope.ServiceProvider.GetRequiredService<IMonitorService>();
                    
                    var checkResult = await monitorService.ExecuteCheckAsync(
                        dueMonitor,
                        stoppingToken);

                    await monitorService.HandleStatusTransitionAsync(
                        dueMonitor,
                        checkResult,
                        stoppingToken);

                    monitorQueue.Schedule(dueMonitor);
                }));
            
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}