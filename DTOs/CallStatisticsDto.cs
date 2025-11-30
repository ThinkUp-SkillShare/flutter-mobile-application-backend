namespace SkillShareBackend.DTOs;

public class CallStatisticsDto
{
    public int TotalCalls { get; set; }
    public int TotalDuration { get; set; }
    public int AverageDuration { get; set; }
    public int MaxParticipants { get; set; }
    public DateTime? LastCall { get; set; }
}

public class UserCallStatisticsDto
{
    public int TotalCallsJoined { get; set; }
    public int TotalTimeInCalls { get; set; }
    public int AverageTimePerCall { get; set; }
    public DateTime? LastCall { get; set; }
}

public class CallStatsResponseDto
{
    public CallStatisticsDto Summary { get; set; } = new();
    public List<object> RecentCalls { get; set; } = new();
}