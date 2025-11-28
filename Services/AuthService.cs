using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;

namespace SkillShareBackend.Services;

/// <summary>
/// Service responsible for authentication-related operations,
/// including login, registration, password verification, and JWT generation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, IConfiguration config, ILogger<AuthService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Performs user login by validating credentials and generating a JWT token.
    /// </summary>
    /// <param name="loginRequest">Login request containing email and password.</param>
    /// <returns>LoginResponseDto containing success status, token, and user info.</returns>
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto loginRequest)
    {
        try
        {
            _logger.LogInformation($"Login attempt for Email: {loginRequest.Email}");

            // Retrieve user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);
            if (user == null)
            {
                _logger.LogWarning($"User not found: {loginRequest.Email}");
                return new LoginResponseDto { Success = false, Message = "Invalid credentials" };
            }

            // Verify password
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning($"Invalid password attempt for: {loginRequest.Email}");
                return new LoginResponseDto { Success = false, Message = "Invalid credentials" };
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                User = new UserResponseDto
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    ProfileImage = user.ProfileImage,
                    CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during login for {loginRequest.Email}");
            return new LoginResponseDto
            {
                Success = false,
                Message = "An error occurred during login"
            };
        }
    }

    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    /// <param name="user">User object containing registration info.</param>
    /// <returns>The created User entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the email already exists.</exception>
    public async Task<User> RegisterAsync(User user)
    {
        // Ensure email is unique
        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            throw new InvalidOperationException("Email already exists");

        // Hash the password before storing
        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

        // Set account creation timestamp
        user.CreatedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Validates a user's email and password without generating a token.
    /// </summary>
    /// <param name="email">User email.</param>
    /// <param name="password">User password.</param>
    /// <returns>True if credentials are valid; otherwise false.</returns>
    public async Task<bool> ValidateUserAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        return user != null && BCrypt.Net.BCrypt.Verify(password, user.Password);
    }

    /// <summary>
    /// Generates a JWT token for a given user.
    /// </summary>
    /// <param name="user">User entity to generate token for.</param>
    /// <returns>JWT token string.</returns>
    public string GenerateJwtToken(User user)
    {
        var jwtSettings = _config.GetSection("Jwt");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims for the JWT token
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim("uid", user.UserId.ToString()),
            new Claim("userId", user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["DurationInMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validates the format of an email address.
    /// </summary>
    /// <param name="email">Email string to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}