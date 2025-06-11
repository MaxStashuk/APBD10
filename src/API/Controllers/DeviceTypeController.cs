using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/devices/types")]
[Authorize(Policy = "UserOrAdmin")]
public class DeviceTypeController : ControllerBase
{
    private readonly MasterContext _context;
    private readonly ILogger<DeviceTypeController> _logger;

    public DeviceTypeController(MasterContext context, ILogger<DeviceTypeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeviceType>>> GetDeviceTypes()
    {
        try
        {
            _logger.LogInformation("Getting all device types");
            var deviceTypes = await _context.DeviceTypes.ToListAsync();
            _logger.LogInformation("Successfully retrieved {Count} device types", deviceTypes.Count);
            return Ok(deviceTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting device types");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
} 