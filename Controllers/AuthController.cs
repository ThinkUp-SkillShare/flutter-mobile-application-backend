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
    private readonly AppDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, AppDbContext context)
    {
        _authService = authService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    /// <param name="user">User entity containing registration data.</param>
    /// <returns>Returns success message and created user ID.</returns>
    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] User user)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        try
        {
            // Call service to register user
            var createdUser = await _authService.RegisterAsync(user);
            return Ok(new
            {
                message = "User registered successfully",
                userId = createdUser.UserId
            });
        }
        catch (InvalidOperationException ex)
        {
            // User already exists
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Log unexpected errors
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Login endpoint.
    /// </summary>
    /// <param name="loginRequest">LoginRequestDto containing email and password.</param>
    /// <returns>Returns JWT token and user information if successful.</returns>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto loginRequest)
    {
        if (!ModelState.IsValid) 
            return BadRequest(new { message = "Invalid request data" });

        // Call service to perform login
        var result = await _authService.LoginAsync(loginRequest);

        if (!result.Success) 
            return Unauthorized(new { message = result.Message });

        return Ok(result);
    }

    /// <summary>
    /// Validate JWT token.
    /// This endpoint is protected by authentication middleware.
    /// If request reaches here, token is valid.
    /// </summary>
    [HttpGet("validate-token")]
    public IActionResult ValidateToken()
    {
        return Ok(new { message = "Token is valid" });
    }
}
