namespace Pingr.Domain.Entities;

/// <summary>
/// Represents a workspace monitor for a specific URL.
/// </summary>
public sealed class Monitor
{
    public Guid Id { get; init; }
    
    public Guid WorkspaceId { get; private set; }
    public Workspace Workspace { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;
    
    public bool Enabled { get; private set; }
    public TimeSpan Interval { get; private set; }

    public string Url { get; private set; } = string.Empty;
    public string HttpMethod { get; private set; } = null!;
    public Dictionary<string, string> HttpHeaders { get; private set; } = new Dictionary<string, string>();
    public string? Body { get; private set; }
    public int TimeoutSeconds { get; private set; }
    public ICollection<int> ExpectedStatusCodes { get; private set; } = new List<int>();
    
    public MonitorCheckResult? LastCheckResult { get; private set; }
    public DateTimeOffset? LastCheckedAt { get; private set; }
    public DateTimeOffset? LastSuccessfulCheckAt { get; private set; }
    
    // Required by EF Core
    private Monitor() { }

    public Monitor(
        Guid workspaceId,
        string name,
        TimeSpan interval,
        string url,
        string method,
        Dictionary<string, string>? httpHeaders,
        string? body,
        int timeoutSeconds,
        ICollection<int> expectedStatusCodes)
    {
        Id = Guid.NewGuid();
        WorkspaceId = workspaceId;
        
        Name = name;
        Enabled = true;
        Interval = interval;
        
        Url = url;
        HttpMethod = method;
        HttpHeaders = httpHeaders ?? HttpHeaders;
        Body = body;
        TimeoutSeconds = timeoutSeconds;
        ExpectedStatusCodes = expectedStatusCodes;
    }

    public void Update(
        string? name,
        TimeSpan? interval,
        string? url,
        string? method,
        Dictionary<string, string>? httpHeaders,
        string? body,
        int? timeoutSeconds,
        ICollection<int>? expectedStatusCodes)
    {
        Name = name ?? Name;
        Interval = interval ?? Interval;
            
        Url = url ?? Url;
        HttpMethod = method ?? HttpMethod;
        HttpHeaders = httpHeaders ?? HttpHeaders;
        Body = body ?? Body;
        TimeoutSeconds = timeoutSeconds ?? TimeoutSeconds;
        ExpectedStatusCodes = expectedStatusCodes ?? ExpectedStatusCodes;
    }
    
    public void Enable() => Enabled = true;
    
    public void Disable() => Enabled = false;
        
    public void UpdateLastCheckResult(MonitorCheckResult newCheckResult)
    {
        Console.WriteLine($"new result: {newCheckResult}");
        LastCheckResult = newCheckResult;
    }

    public void UpdateLastCheckedAt()
        => LastCheckedAt = DateTimeOffset.UtcNow;
    
    public void UpdateLastSuccessfulCheckAt()
        => LastSuccessfulCheckAt = DateTimeOffset.UtcNow;
}

public sealed record MonitorCheckResult(
    MonitorStatus Status,
    int? StatusCode,
    TimeSpan? ResponseTime,
    MonitorFailureReason? FailureReason,
    string? Message,
    DateTimeOffset CheckedAt);

public enum MonitorStatus
{
    Healthy = 0,
    Down = 1
}

public enum MonitorFailureReason
{
    Timeout = 0,
    UnexpectedStatusCode = 1,
    RequestFailed = 2
}