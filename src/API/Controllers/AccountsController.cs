using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace API.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize(Policy = "UserOrAdmin")]
public class AccountsController : ControllerBase
{
    private readonly MasterContext _db;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(MasterContext db, IAuthenticationService authService, ILogger<AccountsController> logger)
    {
        _db = db;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> CreateAccount([FromBody] AccountRegistrationDto dto)
    {
        try
        {
            _logger.LogInformation("Creating new account");

            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto, context, results, true))
                return BadRequest(results);

            if (await _db.Accounts.AnyAsync(a => a.Username == dto.Username))
            {
                _logger.LogWarning("Username {Username} already exists", dto.Username);
                return BadRequest("Username already exists");
            }

            var employee = await _db.Employees.FindAsync(dto.EmployeeId);
            if (employee == null)
            {
                _logger.LogWarning("Employee with ID {EmployeeId} not found", dto.EmployeeId);
                return BadRequest("Employee not found");
            }

            if (await _db.Accounts.AnyAsync(a => a.EmployeeId == dto.EmployeeId))
            {
                _logger.LogWarning("Employee with ID {EmployeeId} already has an account", dto.EmployeeId);
                return BadRequest("Employee already has an account");
            }

            var userRole = await _db.Roles.FirstAsync(r => r.Name == "User");

            var account = new Account
            {
                Username = dto.Username,
                Password = _authService.HashPassword(dto.Password),
                EmployeeId = dto.EmployeeId,
                RoleId = userRole.Id
            };

            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Successfully created account with ID: {Id}", account.Id);
            return Created($"/api/accounts/{account.Id}", new { account.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating account");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<AccountResponseDto>>> GetAllAccounts()
    {
        try
        {
            _logger.LogInformation("Getting all accounts");
            var accounts = await _db.Accounts
                .Select(a => new AccountResponseDto 
                { 
                    Id = a.Id, 
                    Username = a.Username, 
                    Password = a.Password 
                })
                .ToListAsync();
            _logger.LogInformation("Successfully retrieved {Count} accounts", accounts.Count);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting accounts");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AccountResponseDto>> GetAccount(int id)
    {
        try
        {
            _logger.LogInformation("Getting account with ID: {Id}", id);
            var account = await _db.Accounts
                .Include(a => a.Employee)
                .ThenInclude(e => e.Person)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (account == null)
            {
                _logger.LogWarning("Account with ID {Id} not found", id);
                return NotFound();
            }

            var accountId = User.FindFirst("AccountId")?.Value;
            if (!User.IsInRole("Admin") && accountId != id.ToString())
                return Forbid();

            var response = new AccountResponseDto
            {
                Id = account.Id,
                Username = account.Username,
                Password = account.Password,
                Employee = new EmployeeInfoDto
                {
                    FirstName = account.Employee.Person.FirstName,
                    MiddleName = account.Employee.Person.MiddleName,
                    LastName = account.Employee.Person.LastName,
                    Email = account.Employee.Person.Email,
                    PhoneNumber = account.Employee.Person.PhoneNumber
                }
            };

            _logger.LogInformation("Successfully retrieved account with ID: {Id}", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting account with ID: {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateAccount(int id, [FromBody] AccountRegistrationDto dto)
    {
        try
        {
            _logger.LogInformation("Updating account with ID: {Id}", id);
            var account = await _db.Accounts.FindAsync(id);
            if (account == null)
            {
                _logger.LogWarning("Account with ID {Id} not found", id);
                return NotFound();
            }

            var accountId = User.FindFirst("AccountId")?.Value;
            if (!User.IsInRole("Admin") && accountId != id.ToString())
            {
                _logger.LogWarning("User with ID {UserId} is not authorized to update account with ID {AccountId}", User.FindFirst("UserId")?.Value, id);
                return Forbid();
            }

            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto, context, results, true))
                return BadRequest(results);

            if (await _db.Accounts.AnyAsync(a => a.Username == dto.Username && a.Id != id))
            {
                _logger.LogWarning("Username {Username} already exists", dto.Username);
                return BadRequest("Username already exists");
            }

            account.Username = dto.Username;
            account.Password = _authService.HashPassword(dto.Password);

            if (User.IsInRole("Admin") && account.EmployeeId != dto.EmployeeId)
            {
                var employee = await _db.Employees.FindAsync(dto.EmployeeId);
                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found", dto.EmployeeId);
                    return BadRequest("Employee not found");
                }

                if (await _db.Accounts.AnyAsync(a => a.EmployeeId == dto.EmployeeId && a.Id != id))
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} already has an account", dto.EmployeeId);
                    return BadRequest("Employee already has an account");
                }

                account.EmployeeId = dto.EmployeeId;
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully updated account with ID: {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating account with ID: {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        try
        {
            _logger.LogInformation("Deleting account with ID: {Id}", id);
            var account = await _db.Accounts.FindAsync(id);
            if (account == null)
            {
                _logger.LogWarning("Account with ID {Id} not found", id);
                return NotFound();
            }

            _db.Accounts.Remove(account);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted account with ID: {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting account with ID: {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
} 