using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly MasterContext _db;
    private readonly IAuthenticationService _authService;

    public AccountsController(MasterContext db, IAuthenticationService authService)
    {
        _db = db;
        _authService = authService;
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<object>> CreateAccount([FromBody] AccountRegistrationDto dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, context, results, true))
            return BadRequest(results);

        if (await _db.Accounts.AnyAsync(a => a.Username == dto.Username))
            return BadRequest("Username already exists");

        var employee = await _db.Employees.FindAsync(dto.EmployeeId);
        if (employee == null)
            return BadRequest("Employee not found");

        if (await _db.Accounts.AnyAsync(a => a.EmployeeId == dto.EmployeeId))
            return BadRequest("Employee already has an account");

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

        return Created($"/api/accounts/{account.Id}", new { account.Id });
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<AccountResponseDto>>> GetAllAccounts()
    {
        var accounts = await _db.Accounts
            .Select(a => new AccountResponseDto 
            { 
                Id = a.Id, 
                Username = a.Username, 
                Password = a.Password 
            })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AccountResponseDto>> GetAccount(int id)
    {
        var account = await _db.Accounts
            .Include(a => a.Employee)
            .ThenInclude(e => e.Person)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (account == null)
            return NotFound();

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

        return Ok(response);
    }

    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateAccount(int id, [FromBody] AccountRegistrationDto dto)
    {
        var account = await _db.Accounts.FindAsync(id);
        if (account == null)
            return NotFound();

        var accountId = User.FindFirst("AccountId")?.Value;
        if (!User.IsInRole("Admin") && accountId != id.ToString())
            return Forbid();

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, context, results, true))
            return BadRequest(results);

        if (await _db.Accounts.AnyAsync(a => a.Username == dto.Username && a.Id != id))
            return BadRequest("Username already exists");

        account.Username = dto.Username;
        account.Password = _authService.HashPassword(dto.Password);

        if (User.IsInRole("Admin") && account.EmployeeId != dto.EmployeeId)
        {
            var employee = await _db.Employees.FindAsync(dto.EmployeeId);
            if (employee == null)
                return BadRequest("Employee not found");

            if (await _db.Accounts.AnyAsync(a => a.EmployeeId == dto.EmployeeId && a.Id != id))
                return BadRequest("Employee already has an account");

            account.EmployeeId = dto.EmployeeId;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var account = await _db.Accounts.FindAsync(id);
        if (account == null)
            return NotFound();

        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync();

        return NoContent();
    }
} 