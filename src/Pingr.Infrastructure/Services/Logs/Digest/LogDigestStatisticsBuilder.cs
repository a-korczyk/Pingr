using Pingr.Application.Abstractions.Services;
using Pingr.Application.Abstractions.Services.Logs;
using Pingr.Domain.Entities;

namespace Pingr.Infrastructure.Services.Logs.Digest;

/// <summary>
/// Generates statistics based on a workspace's <see cref="LogDigestEntry"/>s.
/// </summary>
public interface ILogDigestStatisticsBuilder
{
    public LogDigestStatistics Build(
        IReadOnlyDictionary<Guid, LogDigestEntry> digestEntries);
}

/// <inheritdoc/>
/// <seealso cref="LogDigestStatistics"/>
public class LogDigestStatisticsBuilder : ILogDigestStatisticsBuilder
{
    public LogDigestStatistics Build(
        IReadOnlyDictionary<Guid, LogDigestEntry> digestEntries)
    {
        int allLogCount = digestEntries.Count;
    
        IReadOnlyDictionary<LogType, int> countByType = 
            digestEntries.Values
                .GroupBy(x => x.Type)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count());

        IReadOnlyDictionary<LogStatus, IReadOnlyDictionary<LogType, int>> countTypeByStatus = 
            digestEntries.Values
                .GroupBy(x => x.Status)
                .ToDictionary(
                    statusGroup => statusGroup.Key,
                    statusGroup => 
                        (IReadOnlyDictionary<LogType, int>) statusGroup
                            .GroupBy(x => x.Type)
                            .ToDictionary(
                                typeGroup => typeGroup.Key,
                                typeGroup => typeGroup.Count()));

        IReadOnlyList<DigestIssue> topIssues =
            digestEntries.Values
                .Where(IsImportantIssue)
                .GroupBy(entry => new
                {
                    entry.Status,
                    entry.Type,
                    entry.Title
                })
                .OrderByDescending(group => group.Count())
                .Take(10)
                .Select(group =>
                    new DigestIssue(
                        group.Key.Status,
                        group.Key.Type,
                        group.Key.Title,
                        group.Count()))
                .ToList();

        return new LogDigestStatistics(
            allLogCount,
            countByType,
            countTypeByStatus,
            topIssues);
    }
    
    /// <summary>
    /// Helper method for checking if an entry is considered an important issue.
    /// </summary>
    /// <param name="entry">The entry to check.</param>
    /// <remarks>
    /// A log must have its status as <c>Pending</c> or <c>InProgress</c> and be of type <c>Warning</c>,
    /// <c>Error</c> or <c>CriticalError</c> to be considered an important issue.
    /// </remarks>
    private static bool IsImportantIssue(LogDigestEntry entry) =>
        (entry.Type == LogType.Warning
         || entry.Type == LogType.Error
         || entry.Type == LogType.CriticalError)
        &&
        (entry.Status == LogStatus.Pending
         || entry.Status == LogStatus.InProgress);
}

/// <param name="AllLogCount">Sum of all entries.</param>
/// <param name="CountByType">Amount of entries of given <see cref="LogType"/>.</param>
/// <param name="CountTypeByStatus">
/// Amount of entries of given <see cref="LogType"/> with given <see cref="LogStatus"/>.
/// </param>
/// <param name="TopIssues">The most important and severe entries.</param>
public sealed record LogDigestStatistics(
    int AllLogCount,
    IReadOnlyDictionary<LogType, int> CountByType,
    IReadOnlyDictionary<LogStatus, IReadOnlyDictionary<LogType, int>> CountTypeByStatus,
    IReadOnlyList<DigestIssue> TopIssues);

/// <summary>
/// Represents a severe and important <see cref="LogDigestEntry"/>.
/// </summary>
/// <param name="Status">Entry's <see cref="LogStatus"/>.</param>
/// <param name="Type">Entry's <see cref="Type"/>.</param>
/// <param name="Title">Entry's title.</param>
/// <param name="Count">Amount of identical entries.</param>
public sealed record DigestIssue(
    LogStatus Status,
    LogType Type,
    string Title,
    int Count);
