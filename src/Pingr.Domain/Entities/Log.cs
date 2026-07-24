using System.Text.Json;

namespace Pingr.Domain.Entities;

/// <summary>
/// Represents a user-created log entry.
/// </summary>
public sealed class Log
{
    public Guid Id { get; private set; }
    
    public Guid WorkspaceId { get; private set; }
    public Workspace Workspace { get; private set; }
    
    public Guid CreatedByUserId { get; private set; }
    public User CreatedByUser { get; private set; }
    
    public LogStatus Status { get; private set; }
    public LogType Type { get; private set; }
    
    public string Title { get; private set; }
    
    // Additional, user-defined fields.
    public JsonDocument Data { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }

    // Required by EF Core
    private Log() { }

    public Log(
        Guid workspaceId,
        Guid createdByUserId,
        LogType type,
        string title,
        JsonDocument data)
    {
        Id = Guid.NewGuid();
        
        WorkspaceId = workspaceId;
        CreatedByUserId = createdByUserId;

        Status = LogStatus.Pending;
        Type = type;
        
        Title = title;
        Data = data;
        
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(
        LogStatus? status = null,
        LogType? type = null,
        string? title = null,
        JsonDocument? data = null)
    {
        Status = status ?? Status;
        Type = type ?? Type;
        Title = title ?? Title;
        Data = data ?? Data;
    }
}

/// <summary>
/// Represents the current state of a log.
/// </summary>
public enum LogStatus
{
    Pending = 0,
    InProgress = 1,
    Resolved = 2,
    
    /// <summary>
    /// The log remains unresolved.
    /// </summary>
    Unresolved = 3,
    
    /// <summary>
    /// Any work on this log has been canceled.
    /// </summary>
    Canceled = 4
}

/// <summary>
/// Represents the log's type.
/// </summary>
public enum LogType
{
    Info = 0,
    Warning = 1,
    Error = 2,
    CriticalError = 3
}