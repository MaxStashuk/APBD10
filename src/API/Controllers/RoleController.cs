using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = "UserOrAdmin")]
public class RoleController : ControllerBase
{
    private readonly MasterContext _context;
    private readonly ILogger<RoleController> _logger;

    public RoleController(MasterContext context, ILogger<RoleController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
    {
        try
        {
            _logger.LogInformation("Getting all roles");
            var roles = await _context.Roles.ToListAsync();
            _logger.LogInformation("Successfully retrieved {Count} roles", roles.Count);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting roles");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
} 