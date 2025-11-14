namespace SkillShareBackend.DTOs
{
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
    }

    public class CreateStudentDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public DateTime? DateBirth { get; set; }
        public string? Country { get; set; }
        public string? EducationalCenter { get; set; }
        public string Gender { get; set; } = "other";
        public int? UserType { get; set; }
        public int? UserId { get; set; }
    }

    public class UpdateStudentDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Nickname { get; set; }
        public DateTime? DateBirth { get; set; }
        public string? Country { get; set; }
        public string? EducationalCenter { get; set; }
        public string? Gender { get; set; }
        public int? UserType { get; set; }
    }
}