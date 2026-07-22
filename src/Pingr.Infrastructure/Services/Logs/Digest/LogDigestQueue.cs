using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Logs;

namespace Pingr.Infrastructure.Services.Logs.Digest;

/// <inheritdoc/>
public sealed class LogDigestQueue(
    ICacheService cacheService) : ILogDigestQueue
{
    public async Task UpsertAsync(
        Guid workspaceId,
        LogDigestEntry entry)
    {
        await cacheService.HashSetAsync(
            $"log-digest-queue:workspaces:{workspaceId}",
            entry.Id.ToString(),
            entry
        );
        
        await cacheService.HashSetIfNotExistsAsync(
            "log-digest-queue:workspaces-list",
            workspaceId.ToString(),
            true);
    }

    public async Task DeleteAsync(
        Guid workspaceId,
        Guid entryId)
    {
        await cacheService.HashDeleteAsync(
            $"log-digest-queue:workspaces:{workspaceId}",
            entryId.ToString());
        
        var recipientDigest = 
            await cacheService
                .HashGetAllAsync<LogDigestEntry>($"log-digest-queue:workspaces:{workspaceId}");

        if (!recipientDigest.Any())
            await cacheService.HashDeleteAsync(
                "log-digest-queue:workspaces-list",
                workspaceId.ToString());
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyDictionary<Guid, LogDigestEntry>>> TakeWorkspacesAsync()
    {
        var result = new Dictionary<Guid, IReadOnlyDictionary<Guid, LogDigestEntry>>();
        
        var workspacesList = 
            await cacheService
                .HashGetAllAsync<bool>("log-digest-queue:workspaces-list");

        foreach (var workspace in workspacesList)
        { 
            var workspaceId = Guid.Parse(workspace.HashKey);
            
            var cachedDigest =
                await cacheService
                    .HashGetAllAsync<LogDigestEntry>($"log-digest-queue:workspaces:{workspaceId}");
            
            var digest = 
               cachedDigest
                .Select(x => 
                    new KeyValuePair<Guid, LogDigestEntry>(
                        Guid.Parse(x.HashKey), 
                        x.Value))
                .ToDictionary();
            
            result.Add(
                workspaceId,
                digest);
            
            // Delete used workspace 
            await cacheService.HashDeleteAsync(
                "log-digest-queue:workspaces-list",
                workspaceId.ToString());
            
            await cacheService.DeleteAsync($"log-digest-queue:workspaces:{workspaceId}");
        }
        
        return result;
    }
}
