namespace SkillShareBackend.DTOs
{
    /// <summary>
    /// DTO representing the response from a login request.
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>
        /// Indicates if login was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message for the client (e.g., error or success message).
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// JWT token generated after successful login.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// User details associated with the login.
        /// </summary>
        public UserResponseDto? User { get; set; }
    }
}