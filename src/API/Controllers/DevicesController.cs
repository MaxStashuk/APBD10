using API.DTOs;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly MasterContext _db;

    public DevicesController(MasterContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Policy = "UserOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DeviceResponseDto>>> GetDevices()
    {
        var devices = await _db.Devices
            .Select(d => new DeviceResponseDto
            {
                Id = d.Id,
                Name = d.Name,
                IsEnabled = d.IsEnabled,
                DeviceTypeName = d.DeviceType.Name
            })
            .ToListAsync();

        return Ok(devices);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "UserOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DeviceResponseDto>> GetDevice(int id)
    {
        var device = await _db.Devices
            .Include(d => d.DeviceType)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null)
            return NotFound();

        var currentUsage = await _db.DeviceEmployees
            .Include(de => de.Employee)
            .ThenInclude(e => e.Person)
            .Where(de => de.DeviceId == id && de.ReturnDate == null)
            .OrderByDescending(de => de.IssueDate)
            .FirstOrDefaultAsync();
        
        var response = new DeviceResponseDto
        {
            Id = device.Id,
            Name = device.Name,
            DeviceTypeName = device.DeviceType?.Name,
            IsEnabled = device.IsEnabled,
            AdditionalProperties = JsonSerializer.Deserialize<object>(device.AdditionalProperties),
            CurrentEmployee = currentUsage == null ? null : new CurrentEmployeeDto
            {
                Id = currentUsage.Employee.Id,
                Name = $"{currentUsage.Employee.Person.FirstName} {currentUsage.Employee.Person.LastName}"
            }
        };

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> CreateDevice([FromBody] DeviceDto dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, context, results, true))
            return BadRequest(results);

        var deviceType = await _db.DeviceTypes.FirstOrDefaultAsync(d => d.Name == dto.DeviceTypeName);
        if (deviceType == null)
            return BadRequest($"Device type '{dto.DeviceTypeName}' is not found");

        var device = new Device
        {
            Name = dto.Name,
            IsEnabled = dto.IsEnabled,
            DeviceTypeId = deviceType.Id,
            AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties ?? new {})
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        return Created($"/api/devices/{device.Id}", new { device.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateDevice(int id, [FromBody] DeviceDto dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, context, results, true))
            return BadRequest(results);

        var device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == id);
        if (device == null)
            return NotFound($"Device with ID {id} not found.");

        var deviceType = await _db.DeviceTypes.FirstOrDefaultAsync(dt => dt.Name == dto.DeviceTypeName);
        if (deviceType == null)
            return BadRequest($"Device type '{dto.DeviceTypeName}' not found.");

        device.Name = dto.Name;
        device.IsEnabled = dto.IsEnabled;
        device.DeviceTypeId = deviceType.Id;
        device.AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties ?? new {});

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteDevice(int id)
    {
        var device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == id);
        if (device == null)
            return NotFound($"Device with ID {id} not found.");
        
        _db.Devices.Remove(device);
        await _db.SaveChangesAsync();
        
        return NoContent();
    }
} 