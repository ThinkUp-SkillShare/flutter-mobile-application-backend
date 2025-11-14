using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models
{
    [Table("subjects")]
    public class Subject
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<StudyGroup> StudyGroups { get; set; } = new List<StudyGroup>();
    }
}