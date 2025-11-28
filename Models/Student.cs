using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models;

[Table("student")]
public class Student
{
    [Key] [Column("id")] public int Id { get; set; }

    [Required]
    [Column("first_name")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Column("last_name")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Column("nickname")]
    [StringLength(100)]
    public string? Nickname { get; set; }

    [Column("date_birth")] public DateTime? DateBirth { get; set; }

    [Column("country")]
    [StringLength(100)]
    public string? Country { get; set; }

    [Column("educational_center")]
    [StringLength(150)]
    public string? EducationalCenter { get; set; }

    [Column("gender")] public string Gender { get; set; } = "other";

    [Column("user_type")] public int? UserType { get; set; }

    [Column("user_id")] public int? UserId { get; set; }

    // Navigation properties
    public User? User { get; set; }
}