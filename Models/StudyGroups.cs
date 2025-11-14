using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models
{
    [Table("study_groups")]
    public class StudyGroup
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [MaxLength(500)]
        [Column("cover_image")]
        public string? CoverImage { get; set; }

        [Required]
        [Column("created_by")]
        public int CreatedBy { get; set; }

        [Column("subject_id")]
        public int? SubjectId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    }
}