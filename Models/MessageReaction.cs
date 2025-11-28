using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models;

[Table("message_reactions")]
public class MessageReaction
{
    [Key] [Column("id")] public int Id { get; set; }

    [Required] [Column("message_id")] public int MessageId { get; set; }

    [Required] [Column("user_id")] public int UserId { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("reaction")]
    public string Reaction { get; set; } = string.Empty;

    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("MessageId")] public virtual GroupMessage? Message { get; set; }

    [ForeignKey("UserId")] public virtual User? User { get; set; }
}