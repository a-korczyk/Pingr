using Microsoft.Extensions.DependencyInjection;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Application.Abstractions.Services;
using Monitor = Pingr.Domain.Entities.Monitor;

namespace Pingr.Infrastructure.Services.Monitors;

/// <inheritdoc/>
public sealed class MonitorQueue(
        IServiceScopeFactory serviceScopeFactory) : IMonitorQueue
{
    private readonly Lock _lock  = new();
    
    /// <remarks>
    /// The elements are monitor identifiers.
    /// The priority is the monitor's next check time.
    /// </remarks>
    private readonly PriorityQueue<Guid, DateTimeOffset> _scheduledMonitors = new();

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var monitorRepository = scope.ServiceProvider.GetRequiredService<IMonitorRepository>();
        
        var currentPage = 1;
        var pageSize = 100;

        while (true)
        {
            var page = await monitorRepository.GetAllEnabledAsync(
                new Pagination(
                    currentPage,
                    pageSize),
                cancellationToken);

            if (!page.Any())
                break;

            foreach (var monitor in page)
            {
                // Space out monitors at startup
                var nextCheckAt =
                    DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(0,
                        15)); //todo: relative spacing (30 int -> 0-10, 1min int -> 0-30)

                lock (_lock)
                {
                    _scheduledMonitors.Enqueue(monitor.Id, nextCheckAt);
                }
            }

            // Break if count is smaller than page size since that
            // means there are no more monitors left
            if (page.Count < pageSize)
                break;

            currentPage++;
        }
    }

    public void Add(Monitor monitor)
    {
        lock (_lock)
        {
            _scheduledMonitors.Enqueue(monitor.Id, DateTimeOffset.UtcNow);
        }
    }

    public async Task<ICollection<Monitor>> TakeDueMonitorsAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var monitorRepository = scope.ServiceProvider.GetRequiredService<IMonitorRepository>();
        
        var dueMonitors = new List<Monitor>();
        var now = DateTimeOffset.UtcNow;

        while (true)
        {
            Guid monitorId;
            
            lock (_lock)
            {
                if (_scheduledMonitors.TryPeek(out monitorId, out var scheduledAt) is false)
                    break;
                
                if (scheduledAt > now) 
                    break;
                
                _scheduledMonitors.Dequeue();
            }
        
            var monitor = await monitorRepository.GetByIdAsync(
                monitorId,
                cancellationToken);
            
            if (monitor is null
                || monitor.Enabled is false)
                continue;
        
            dueMonitors.Add(monitor);
        }
    
        return dueMonitors;
    }

    public void Schedule(Monitor monitor)
    {
        lock (_lock)
        {
            _scheduledMonitors.Enqueue(monitor.Id, GetNextCheckAt(monitor.Interval));
        }
    }

    private DateTimeOffset GetNextCheckAt(TimeSpan interval)
    {
        return DateTimeOffset.UtcNow
            .Add(interval);
    }
}