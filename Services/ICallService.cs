namespace SkillShareBackend.Services;

public interface ICallService
{
    Task<CallRoomResult> CreateCallRoom(int groupId, string userId);
    Task<JoinCallResult> JoinCall(int groupId, string userId);
    Task<ActiveCallResult> GetActiveCall(int groupId);
    Task<bool> EndCall(int groupId, string userId);
    Task<CallStatsResult> GetCallStats(int groupId);
    Task<UserCallStatsResult> GetUserCallStats(string userId);
}

public class CallRoomResult
{
    public bool Success { get; set; }
    public string? CallId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class JoinCallResult
{
    public bool Success { get; set; }
    public string? CallId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ActiveCallResult
{
    public bool Success { get; set; }
    public string? CallId { get; set; }
    public bool IsActive { get; set; }
    public int ParticipantCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CallStatsResult
{
    public bool Success { get; set; }
    public int TotalCalls { get; set; }
    public int TotalParticipants { get; set; }
    public int AverageDuration { get; set; }
    public TimeSpan? TotalDuration { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UserCallStatsResult
{
    public bool Success { get; set; }
    public int TotalCalls { get; set; }
    public TimeSpan TotalCallTime { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CallSession
{
    public string CallId { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public List<string> Participants { get; set; } = new();
}