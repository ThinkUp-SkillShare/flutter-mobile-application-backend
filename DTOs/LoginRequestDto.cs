using System.ComponentModel.DataAnnotations;

namespace SkillShareBackend.DTOs;

/// <summary>
/// Data Transfer Object for user login requests.
/// Includes email and password validation.
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// User email address. Must be a valid email format.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email format must be example@example.com")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password. Minimum length is 8 characters.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;
}