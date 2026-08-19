using Microsoft.EntityFrameworkCore;
using Pingr.Application.Abstractions.Repositories;
using Pingr.Domain.Entities;
using Monitor = Pingr.Domain.Entities.Monitor;

namespace Pingr.Infrastructure.Repositories;

/// <inheritdoc/>
public sealed class MonitorRepository(
    ApplicationDbContext dbContext) : IMonitorRepository
{
    public async Task AddAsync(
        Monitor monitor,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Monitors.AddAsync(monitor, cancellationToken);
    }

    public async Task<Monitor?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Monitors.FindAsync(id, cancellationToken);
    }

    public async Task<ICollection<Monitor>> GetByWorkspaceIdAsync(
        Guid workspaceId,
        Pagination pagination,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Monitors
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.WorkspaceId)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<ICollection<Monitor>> GetEnabledByWorkspaceIdAsync(
        Guid workspaceId,
        Pagination pagination,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Monitors
            .Where(x => 
                x.WorkspaceId == workspaceId
                && x.Enabled == true)
            .OrderByDescending(x => x.WorkspaceId)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<ICollection<Monitor>> GetAllEnabledAsync(
        Pagination pagination,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Monitors
            .Where(x => x.Enabled == true)
            .OrderByDescending(x => x.WorkspaceId)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);
    }
    
    public void Delete(Monitor monitor)
    {
        dbContext.Monitors.Remove(monitor);
    }

    public async Task UpdateLastCheckResultAsync(
        Monitor monitor,
        MonitorCheckResult newCheckResult,
        CancellationToken cancellationToken = default)
    {
        dbContext.Attach(monitor);
        
        monitor.UpdateLastCheckResult(newCheckResult);
        
        dbContext.Monitors
            .Entry(monitor)
            .ComplexProperty(x => x.LastCheckResult)
            .IsModified = true;
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}