using Pingr.Domain.Entities;
using Monitor = Pingr.Domain.Entities.Monitor;

namespace Pingr.Application.Abstractions.Repositories;

/// <remarks>
/// <see cref="AddAsync"/> and <see cref="Delete"/> do not
/// save the current unit's of works changes.
/// </remarks>
public interface IMonitorRepository
{
    Task AddAsync(
        Monitor monitor,
        CancellationToken cancellationToken);
    
    Task<Monitor?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);
    
    Task<ICollection<Monitor>> GetByWorkspaceIdAsync(
        Guid workspaceId,
        Pagination pagination,
        CancellationToken cancellationToken);
    
    Task<ICollection<Monitor>> GetEnabledByWorkspaceIdAsync(
        Guid workspaceId,
        Pagination pagination,
        CancellationToken cancellationToken);
    
    Task<ICollection<Monitor>> GetAllEnabledAsync(
        Pagination pagination,
        CancellationToken cancellationToken);

    public void Delete(
        Monitor monitor);

    public Task UpdateLastCheckResultAsync(
        Monitor monitor,
        MonitorCheckResult newCheckResult,
        CancellationToken cancellationToken);
}