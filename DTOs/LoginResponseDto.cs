namespace SkillShareBackend.DTOs
{
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public UserResponseDto? User { get; set; }
    }

    public class UserResponseDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}