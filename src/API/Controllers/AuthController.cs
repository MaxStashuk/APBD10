using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly MasterContext _context;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(MasterContext context, IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(LoginDto loginDto)
    {
        try
        {
            _logger.LogInformation("Attempting login for user: {Username}", loginDto.Username);
            var account = await _context.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == loginDto.Username);

            if (account == null || account.Password != loginDto.Password)
            {
                _logger.LogWarning("Invalid login attempt for user: {Username}", loginDto.Username);
                return Unauthorized(new { error = "Invalid username or password" });
            }

            if (account.Role == null)
            {
                _logger.LogWarning("User {Username} has no role assigned", loginDto.Username);
                return Unauthorized(new { error = "User has no role assigned" });
            }

            var token = _authService.GenerateJwtToken(account, account.Role.Name);
            _logger.LogInformation("Successful login for user: {Username}", loginDto.Username);
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for user: {Username}", loginDto.Username);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("validate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken()
    {
        return Ok(new { message = "Token is valid" });
    }
} 