using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;
using SkillShareBackend.Services;

namespace SkillShareBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _context; // Agregar el DbContext
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, AppDbContext context)
    {
        _authService = authService;
        _logger = logger;
        _context = context; // Inyectar el DbContext
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto loginRequest)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "Invalid request data" });

        var result = await _authService.LoginAsync(loginRequest);

        if (!result.Success) return Unauthorized(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] User user)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var createdUser = await _authService.RegisterAsync(user);
            return Ok(new
            {
                message = "User registered successfully",
                userId = createdUser.UserId
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpGet("validate-token")]
    public IActionResult ValidateToken()
    {
        // This endpoint is protected by JWT authentication
        // If the request reaches here, the token is valid
        return Ok(new { message = "Token is valid" });
    }

    [HttpGet("test-connection")]
    public IActionResult TestConnection()
    {
        return Ok(new
        {
            message = "Backend is running successfully",
            timestamp = DateTime.UtcNow,
            status = "OK"
        });
    }

    [HttpPost("create-test-user")]
    public async Task<ActionResult> CreateTestUser([FromBody] LoginRequestDto userRequest)
    {
        try
        {
            // Verificar si el usuario ya existe
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userRequest.Email);
            if (existingUser != null) return Conflict(new { message = "User already exists" });

            // Crear nuevo usuario con contraseña hasheada
            var newUser = new User
            {
                Email = userRequest.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(userRequest.Password),
                CreatedAt = DateTime.UtcNow // Usar DateTime en lugar de string
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Test user created successfully",
                userId = newUser.UserId,
                email = newUser.Email
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test user");
            return StatusCode(500, new { message = "An error occurred creating test user" });
        }
    }

    [HttpGet("generate-hash/{password}")]
    public IActionResult GenerateHash(string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        return Ok(new { password, hash });
    }

    [HttpGet("list-users")]
    public async Task<ActionResult> ListUsers()
    {
        var users = await _context.Users
            .Select(u => new { u.UserId, u.Email, u.Password, u.CreatedAt })
            .ToListAsync();

        return Ok(users);
    }
}