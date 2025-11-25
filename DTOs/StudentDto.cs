using System.ComponentModel.DataAnnotations;

namespace SkillShareBackend.DTOs
{
    public class CreateStudentDto
    {
        [Required]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Only letters are allowed")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Only letters are allowed")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 3)]
        public string? Nickname { get; set; }

        public DateTime? DateBirth { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(150)]
        public string? EducationalCenter { get; set; }

        [RegularExpression("^(male|female|other|prefer_not_to_say)$")]
        public string Gender { get; set; } = "other";

        [Range(1, 4)]
        public int? UserType { get; set; }

        [Required]
        public int UserId { get; set; }
    }

    public class UpdateStudentDto
    {
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Only letters are allowed")]
        public string? FirstName { get; set; }

        [StringLength(100)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Only letters are allowed")]
        public string? LastName { get; set; }

        [StringLength(100, MinimumLength = 3)]
        public string? Nickname { get; set; }

        public DateTime? DateBirth { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(150)]
        public string? EducationalCenter { get; set; }

        [RegularExpression("^(male|female|other|prefer_not_to_say)$")]
        public string? Gender { get; set; }

        [Range(1, 4)]
        public int? UserType { get; set; }

        public string? ProfileImage { get; set; } // Añadir este campo
    }

    public class StudentDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public DateTime? DateBirth { get; set; }
        public string? Country { get; set; }
        public string? EducationalCenter { get; set; }
        public string Gender { get; set; } = "other";
        public int? UserType { get; set; }
        public int? UserId { get; set; }
        
        // Añadir propiedades del User
        public UserDto? User { get; set; }
    }

    // Crear un DTO para User
    public class UserDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}