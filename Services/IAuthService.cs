using SkillShareBackend.DTOs;
using SkillShareBackend.Models;

namespace SkillShareBackend.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto loginRequest);
    Task<User> RegisterAsync(User user);
    Task<bool> ValidateUserAsync(string email, string password);
    string GenerateJwtToken(User user);
}