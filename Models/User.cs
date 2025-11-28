using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillShareBackend.Models;

[Table("users")]
public class User
{
    [Key] [Column("user_id")] public int UserId { get; set; }

    [Required]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format")]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Column("profile_image")] public string? ProfileImage { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; } // Cambiar de string a DateTime
}