using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models
{
    [Table("group_members")]
    public class GroupMember
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
        [MaxLength(10)]
        [Column("role")]
        public string Role { get; set; } = "member"; // 'admin' or 'member'

        // Navigation properties
        [ForeignKey("GroupId")]
        public virtual StudyGroup? Group { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}