using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models;

[Table("group_messages")]
public class GroupMessage
{
    [Key] 
    [Column("id")] 
    public int Id { get; set; }

    [Required] 
    [Column("group_id")] 
    public int GroupId { get; set; }

    [Required] 
    [Column("user_id")] 
    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("message_type")]
    public string MessageType { get; set; } = "text"; 

    [Column("content")] 
    public string? Content { get; set; }

    [MaxLength(2000)] 
    [Column("file_url")] 
    public string? FileUrl { get; set; }

    [MaxLength(255)] 
    [Column("file_name")] 
    public string? FileName { get; set; }

    [Column("file_size")] 
    public long? FileSize { get; set; }

    [Column("duration")] 
    public int? Duration { get; set; } // For audio in seconds

    [Column("reply_to_message_id")] 
    public int? ReplyToMessageId { get; set; }

    [Column("is_edited")] 
    public bool IsEdited { get; set; } = false;

    [Column("is_deleted")] 
    public bool IsDeleted { get; set; } = false;

    [Column("created_at")] 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")] 
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("GroupId")] 
    public virtual StudyGroup? Group { get; set; }

    [ForeignKey("UserId")] 
    public virtual User? User { get; set; }

    [ForeignKey("ReplyToMessageId")] 
    public virtual GroupMessage? ReplyToMessage { get; set; }

    public virtual ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    public virtual ICollection<MessageReadStatus> ReadStatuses { get; set; } = new List<MessageReadStatus>();
}