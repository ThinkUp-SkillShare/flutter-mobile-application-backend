namespace SkillShareBackend.DTOs
{
    /// <summary>
    /// DTO representing user information sent in responses.
    /// </summary>
    public class UserResponseDto
    {
        /// <summary>
        /// Unique identifier for the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// User's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// URL or path to the user's profile image (optional).
        /// </summary>
        public string? ProfileImage { get; set; }

        /// <summary>
        /// Timestamp when the user was created.
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;
    }
}