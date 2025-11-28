using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models;

[Table("GROUP_CALLS")]
public class GroupCall
{
    [Key] [Column("id")] public int Id { get; set; }

    [Required] [Column("call_id")] public string CallId { get; set; } = string.Empty;

    [Required] [Column("group_id")] public int GroupId { get; set; }

    [ForeignKey("GroupId")] public StudyGroup Group { get; set; } = null!;

    [Required] [Column("started_by")] public int StartedBy { get; set; }

    [ForeignKey("StartedBy")] public User User { get; set; } = null!;

    [Required] [Column("started_at")] public DateTime StartedAt { get; set; }

    [Column("ended_at")] public DateTime? EndedAt { get; set; }

    [Required] [Column("is_active")] public bool IsActive { get; set; } = true;

    // Propiedad calculada para la duración
    [NotMapped] public int Duration => EndedAt.HasValue ? (int)(EndedAt.Value - StartedAt).TotalSeconds : 0;

    // Navegación a participantes
    public virtual ICollection<CallParticipant> Participants { get; set; } = new List<CallParticipant>();
}

[Table("CALL_PARTICIPANTS")]
public class CallParticipant
{
    [Key] [Column("id")] public int Id { get; set; }

    [Required] [Column("call_id")] public string CallId { get; set; } = string.Empty;

    [Required] [Column("user_id")] public int UserId { get; set; }

    [ForeignKey("UserId")] public User User { get; set; } = null!;

    [Required] [Column("joined_at")] public DateTime JoinedAt { get; set; }

    [Column("left_at")] public DateTime? LeftAt { get; set; }

    // Propiedad calculada para la duración de participación
    [NotMapped] public int Duration => LeftAt.HasValue ? (int)(LeftAt.Value - JoinedAt).TotalSeconds : 0;

    // Navegación a la llamada
    [ForeignKey("CallId")] public virtual GroupCall GroupCall { get; set; } = null!;
}

[Table("CALL_STATISTICS")]
public class CallStatistics
{
    [Key] [Column("id")] public int Id { get; set; }

    [Required] [Column("call_id")] public string CallId { get; set; } = string.Empty;

    [Required] [Column("group_id")] public int GroupId { get; set; }

    [Column("total_participants")] public int TotalParticipants { get; set; }

    [Column("average_duration")] public int AverageDuration { get; set; }

    [Column("max_participants")] public int MaxParticipants { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [ForeignKey("CallId")] public virtual GroupCall GroupCall { get; set; } = null!;

    [ForeignKey("GroupId")] public virtual StudyGroup Group { get; set; } = null!;
}