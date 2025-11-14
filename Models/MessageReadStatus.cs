using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models
{
    [Table("message_read_status")]
    public class MessageReadStatus
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("message_id")]
        public int MessageId { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("read_at")]
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("MessageId")]
        public virtual GroupMessage? Message { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}