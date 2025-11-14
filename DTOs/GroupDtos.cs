namespace SkillShareBackend.DTOs
{
    public class StudyGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public int CreatedBy { get; set; }
        public string? CreatorName { get; set; }
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
        public string? UserRole { get; set; } // Role of the current user in this group
    }

    public class CreateStudyGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public int? SubjectId { get; set; }
    }

    public class UpdateStudyGroupDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public int? SubjectId { get; set; }
    }

    public class JoinGroupDto
    {
        public int GroupId { get; set; }
    }

    public class GroupMemberDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string Role { get; set; } = "member";
    }

    public class SubjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}