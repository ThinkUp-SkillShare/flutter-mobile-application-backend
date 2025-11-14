using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace SkillShareBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IConfiguration config, ILogger<AuthService> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto loginRequest)
{
    try
    {
        _logger.LogInformation($"🔐 LOGIN ATTEMPT - Email: {loginRequest.Email}");

        // Validate email format
        if (!IsValidEmail(loginRequest.Email))
        {
            _logger.LogWarning($"❌ INVALID EMAIL FORMAT: {loginRequest.Email}");
            return new LoginResponseDto
            {
                Success = false,
                Message = "Invalid email format"
            };
        }

        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

        if (user == null)
        {
            _logger.LogWarning($"❌ USER NOT FOUND: {loginRequest.Email}");
            return new LoginResponseDto
            {
                Success = false,
                Message = "Invalid credentials"
            };
        }

        _logger.LogInformation($"✅ USER FOUND - UserId: {user.UserId}, Email: {user.Email}");

        // Verify password using BCrypt
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password);
        
        _logger.LogInformation($"🔑 PASSWORD VERIFICATION RESULT: {isPasswordValid}");

        if (!isPasswordValid)
        {
            _logger.LogWarning($"❌ INVALID PASSWORD for: {loginRequest.Email}");
            return new LoginResponseDto
            {
                Success = false,
                Message = "Invalid credentials"
            };
        }

        // Generate JWT token
        var token = GenerateJwtToken(user);

        _logger.LogInformation($"🎉 SUCCESSFUL LOGIN - User: {user.Email}, UserId: {user.UserId}");

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
                CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") // Convertir DateTime a string
            }
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"💥 ERROR DURING LOGIN for {loginRequest.Email}");
        return new LoginResponseDto
        {
            Success = false,
            Message = "An error occurred during login"
        };
    }
}

        public async Task<User> RegisterAsync(User user)
        {
            // Validate unique email
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            // Hash password
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
    
            // Set created date
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> ValidateUserAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false;

            return BCrypt.Net.BCrypt.Verify(password, user.Password);
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim("userId", user.UserId.ToString()),
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

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}