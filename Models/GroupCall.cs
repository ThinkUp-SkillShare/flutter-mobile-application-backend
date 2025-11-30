using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models;

[Table("GROUP_CALLS")]
public class Call
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("call_id")]
    [MaxLength(100)]
    public string CallId { get; set; } = string.Empty;

    [Required]
    [Column("group_id")]
    public int GroupId { get; set; }

    [ForeignKey("GroupId")]
    public StudyGroup? Group { get; set; }

    [Required]
    [Column("started_by")]
    [MaxLength(100)]
    public string StartedBy { get; set; } = string.Empty;

    [Required]
    [Column("started_at")]
    public DateTime StartedAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [Column("ended_by")]
    [MaxLength(100)]
    public string? EndedBy { get; set; }

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("participant_count")]
    public int ParticipantCount { get; set; } = 0;

    [NotMapped]
    public TimeSpan? Duration => EndedAt.HasValue ? EndedAt.Value - StartedAt : null;

    public virtual ICollection<CallParticipant> Participants { get; set; } = new List<CallParticipant>();
}

[Table("CALL_PARTICIPANTS")]
public class CallParticipant
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("call_id")]
    [MaxLength(100)]
    public string CallId { get; set; } = string.Empty;

    [Required]
    [Column("user_id")]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }

    [Column("left_at")]
    public DateTime? LeftAt { get; set; }

    [NotMapped]
    public TimeSpan? Duration => LeftAt.HasValue ? LeftAt.Value - JoinedAt : null;

    [ForeignKey("CallId")]
    public virtual Call? Call { get; set; }
}

[Table("CALL_STATISTICS")]
public class CallStatistics
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("call_id")]
    [MaxLength(100)]
    public string CallId { get; set; } = string.Empty;

    [Required]
    [Column("group_id")]
    public int GroupId { get; set; }

    [Column("total_participants")]
    public int TotalParticipants { get; set; }

    [Column("average_duration")]
    public int AverageDuration { get; set; }

    [Column("max_participants")]
    public int MaxParticipants { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("CallId")]
    public virtual Call? Call { get; set; }

    [ForeignKey("GroupId")]
    public virtual StudyGroup? Group { get; set; }
}