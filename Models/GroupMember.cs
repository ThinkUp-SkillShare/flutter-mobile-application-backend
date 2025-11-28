using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models;

/// <summary>
///     Represents the relationship between a user and a study group.
///     Includes the member's role inside the group (member/admin).
/// </summary>
[Table("group_members")]
public class GroupMember
{
    /// <summary>
    ///     Primary key of the group member entry.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    ///     ID of the group this membership belongs to.
    /// </summary>
    [Required]
    [Column("group_id")]
    public int GroupId { get; set; }

    /// <summary>
    ///     ID of the user who is a member of the group.
    /// </summary>
    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>
    ///     Represents the member's role inside the group ("admin" or "member").
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column("role")]
    public string Role { get; set; } = "member";

    /// <summary>
    ///     Navigation property to the related StudyGroup entity.
    /// </summary>
    [ForeignKey("GroupId")]
    public virtual StudyGroup? Group { get; set; }

    /// <summary>
    ///     Navigation property to the related User entity.
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}