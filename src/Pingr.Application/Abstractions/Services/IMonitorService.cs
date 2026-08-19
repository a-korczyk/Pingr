using Pingr.Domain.Entities;
using Monitor = Pingr.Domain.Entities.Monitor;

namespace Pingr.Application.Abstractions.Services;

public interface IMonitorService
{
    Task<MonitorCheckResult> ExecuteCheckAsync(
        Monitor monitor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Handles a monitor's status transition.
    /// If it has changed to a new status it'll send a notification.
    /// </summary>
    /// <remarks>Meant to be called after <see cref="ExecuteCheckAsync"/></remarks>
    Task HandleStatusTransitionAsync(
        Monitor monitor,
        MonitorCheckResult checkResult,
        CancellationToken cancellationToken);
}
