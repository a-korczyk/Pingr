using Monitor = Pingr.Domain.Entities.Monitor;

namespace Pingr.Application.Abstractions.Services;

/// <remarks>
/// Doesn't contain Update or Delete methods because <see cref="TakeDueMonitorsAsync"/>
/// always returns the latest monitor details. <see cref="TakeDueMonitorsAsync"/> dequeues
/// the identifier and if it doesn't find any monitor with it then it doesn't return it in the
/// collection.
/// </remarks>
public interface IMonitorQueue
{
    Task InitializeAsync(
        CancellationToken cancellationToken);

    void Add(
        Monitor monitor);

    /// <remarks>
    /// Does not automatically requeue the monitors.
    /// Use <see cref="ScheduleNext"/>
    /// </remarks>
    Task<ICollection<Monitor>> TakeDueMonitorsAsync(
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Queues the provided monitor and sets it's next check time. 
    /// </summary>
    void Schedule(
        Monitor monitor);
}