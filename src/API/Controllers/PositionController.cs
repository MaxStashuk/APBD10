using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "UserOrAdmin")]
public class PositionController : ControllerBase
{
    private readonly MasterContext _context;
    private readonly ILogger<PositionController> _logger;

    public PositionController(MasterContext context, ILogger<PositionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Position>>> GetPositions()
    {
        try
        {
            _logger.LogInformation("Getting all positions");
            var positions = await _context.Positions.ToListAsync();
            _logger.LogInformation("Successfully retrieved {Count} positions", positions.Count);
            return Ok(positions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting positions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
} 