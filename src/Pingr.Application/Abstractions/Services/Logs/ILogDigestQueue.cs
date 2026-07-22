using Pingr.Domain.Entities;

namespace Pingr.Application.Abstractions.Services.Logs;

/// <summary>
/// Provides methods for manipulating and retrieving the entry queue for log digests.
/// </summary>
public interface ILogDigestQueue
{
    /// <summary>
    /// Adds a new entry to the workspace's digest queue
    /// or overwrites the existing one.
    /// </summary>
    /// <param name="workspaceId">Workspace's identifier.</param>
    /// <param name="entry">Entry to add.</param>
    public Task UpsertAsync(
        Guid workspaceId,
        LogDigestEntry entry);

    /// <summary>
    /// Deletes the existing entry with the provided identifier.
    /// </summary>
    /// <param name="workspaceId">Workspace's identifier.</param>
    /// <param name="entryId">Entry's identifier.</param>
    public Task DeleteAsync(
        Guid workspaceId,
        Guid entryId);

    /// <summary>
    /// Returns workspaces and their digest entries, then clears it.
    /// </summary>
    /// <returns>
    /// A read-only dictionary, where the key is the workspace's identifier and the value
    /// is a read-only dictionary where the key is the log's identifier and the value is
    /// a <see cref="LogDigestEntry"/>.
    /// </returns>
    public Task<IReadOnlyDictionary<Guid, IReadOnlyDictionary<Guid, LogDigestEntry>>> TakeWorkspacesAsync();
}

/// <summary>
/// Represents a log entry in a workspace's digest queue.
/// </summary>
/// <param name="Id">Identifier of the log.</param>
/// <param name="Status">The log's status.</param>
/// <param name="Type">The log's type.</param>
/// <param name="Title">The log's title.</param>
public sealed record LogDigestEntry(
    Guid Id,
    LogStatus Status,
    LogType Type,
    string Title);