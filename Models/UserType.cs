using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models;

[Table("user_type")]
public class UserType
{
    [Key] [Column("id")] public int Id { get; set; }

    [Required]
    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;
}