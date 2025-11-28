using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models;

/// <summary>
///     Represents a study group in the application.
///     Contains metadata about the group, its subject, creator, and members.
/// </summary>
[Table("study_groups")]
public class StudyGroup
{
    /// <summary>
    ///     Primary key of the study group.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    ///     Name of the study group.
    /// </summary>
    [Required]
    [MaxLength(150)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Optional description providing more details about the group.
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    ///     Optional URL to a cover image for the group.
    /// </summary>
    [MaxLength(500)]
    [Column("cover_image")]
    public string? CoverImage { get; set; }

    /// <summary>
    ///     ID of the user who created the group.
    /// </summary>
    [Required]
    [Column("created_by")]
    public int CreatedBy { get; set; }

    /// <summary>
    ///     Optional subject ID associated with the group.
    /// </summary>
    [Column("subject_id")]
    public int? SubjectId { get; set; }

    /// <summary>
    ///     Timestamp of when the group was created (UTC).
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    ///     Reference to the user who created the group.
    /// </summary>
    [ForeignKey("CreatedBy")]
    public virtual User? Creator { get; set; }

    /// <summary>
    ///     Reference to the subject this group belongs to.
    /// </summary>
    [ForeignKey("SubjectId")]
    public virtual Subject? Subject { get; set; }

    /// <summary>
    ///     Collection of members in this group.
    /// </summary>
    public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}