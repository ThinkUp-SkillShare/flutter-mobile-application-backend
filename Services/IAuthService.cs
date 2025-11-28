using SkillShareBackend.DTOs;
using SkillShareBackend.Models;

namespace SkillShareBackend.Services;

/// <summary>
/// Interface defining authentication-related operations,
/// including login, registration, credential validation, and JWT generation.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Performs user login by validating credentials and generating a JWT token.
    /// </summary>
    /// <param name="loginRequest">Login request containing email and password.</param>
    /// <returns>LoginResponseDto with success status, token, and user information.</returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto loginRequest);

    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    /// <param name="user">User entity containing registration data.</param>
    /// <returns>The created User entity.</returns>
    Task<User> RegisterAsync(User user);

    /// <summary>
    /// Validates a user's email and password without generating a token.
    /// </summary>
    /// <param name="email">User email.</param>
    /// <param name="password">User password.</param>
    /// <returns>True if credentials are valid; otherwise false.</returns>
    Task<bool> ValidateUserAsync(string email, string password);

    /// <summary>
    /// Generates a JWT token for a given user.
    /// </summary>
    /// <param name="user">User entity to generate a token for.</param>
    /// <returns>JWT token string.</returns>
    string GenerateJwtToken(User user);
}