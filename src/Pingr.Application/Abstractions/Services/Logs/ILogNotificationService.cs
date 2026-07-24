using Pingr.Domain.Entities;

namespace Pingr.Application.Abstractions.Services.Logs;

/// <summary>
/// Provides methods for sending log-related notifications.
/// </summary>
public interface ILogNotificationService
{
    /// <summary>
    /// Sends a notification about a critical error.
    /// </summary>
    /// <param name="log">The critical error's log.</param>
    /// <param name="user">The user to notify.</param>
    Task NotifyCriticalErrorAsync(
        Log log,
        User user,
        CancellationToken cancellationToken);
}