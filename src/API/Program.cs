using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                       throw new InvalidOperationException("No connection string found");

builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key");
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer");
        var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));
    options.AddPolicy("UserOrAdmin", policy => 
        policy.RequireClaim(ClaimTypes.Role, new[] { "User", "Admin" }));
});

builder.Services.AddDbContext<MasterContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IAuthenticationService, JwtAuthenticationService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MasterContext>();
    if (!await context.Roles.AnyAsync())
    {
        context.Roles.AddRange(
            new Role { Name = "Admin" },
            new Role { Name = "User" }
        );
        await context.SaveChangesAsync();
    }
}

app.MapControllers();

app.MapPost("/api/accounts", async (AccountRegistrationDto dto, MasterContext db, IAuthenticationService authService, ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Admin"))
        return Results.Forbid();

    var context = new ValidationContext(dto);
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(dto, context, results, true))
        return Results.BadRequest(results);

    if (await db.Accounts.AnyAsync(a => a.Username == dto.Username))
        return Results.BadRequest("Username already exists");

    var employee = await db.Employees.FindAsync(dto.EmployeeId);
    if (employee == null)
        return Results.BadRequest("Employee not found");

    if (await db.Accounts.AnyAsync(a => a.EmployeeId == dto.EmployeeId))
        return Results.BadRequest("Employee already has an account");

    var userRole = await db.Roles.FirstAsync(r => r.Name == "User");

    var account = new Account
    {
        Username = dto.Username,
        Password = authService.HashPassword(dto.Password),
        EmployeeId = dto.EmployeeId,
        RoleId = userRole.Id
    };

    db.Accounts.Add(account);
    await db.SaveChangesAsync();

    return Results.Created($"/api/accounts/{account.Id}", new { account.Id });
})
.WithName("CreateAccount")
.WithOpenApi()
.RequireAuthorization("AdminOnly");

app.MapGet("/api/accounts", async (MasterContext db, ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Admin"))
        return Results.Forbid();

    var accounts = await db.Accounts
        .Select(a => new AccountResponseDto 
        { 
            Id = a.Id, 
            Username = a.Username, 
            Password = a.Password 
        })
        .ToListAsync();

    return Results.Ok(accounts);
})
.WithName("GetAllAccounts")
.WithOpenApi()
.RequireAuthorization("AdminOnly");

app.MapGet("/api/accounts/{id}", async (int id, MasterContext db, ClaimsPrincipal user) =>
{
    var account = await db.Accounts
        .Include(a => a.Employee)
        .ThenInclude(e => e.Person)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (account == null)
        return Results.NotFound();

    var accountId = user.FindFirst("AccountId")?.Value;
    if (!user.IsInRole("Admin") && accountId != id.ToString())
        return Results.Forbid();

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

    return Results.Ok(response);
})
.WithName("GetAccount")
.WithOpenApi()
.RequireAuthorization();

app.MapPut("/api/accounts/{id}", async (int id, AccountRegistrationDto dto, MasterContext db, IAuthenticationService authService, ClaimsPrincipal user) =>
{
    var account = await db.Accounts.FindAsync(id);
    if (account == null)
        return Results.NotFound();

    var accountId = user.FindFirst("AccountId")?.Value;
    if (!user.IsInRole("Admin") && accountId != id.ToString())
        return Results.Forbid();

    var context = new ValidationContext(dto);
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(dto, context, results, true))
        return Results.BadRequest(results);

    if (await db.Accounts.AnyAsync(a => a.Username == dto.Username && a.Id != id))
        return Results.BadRequest("Username already exists");

    account.Username = dto.Username;
    account.Password = authService.HashPassword(dto.Password);

    if (user.IsInRole("Admin") && account.EmployeeId != dto.EmployeeId)
    {
        var employee = await db.Employees.FindAsync(dto.EmployeeId);
        if (employee == null)
            return Results.BadRequest("Employee not found");

        if (await db.Accounts.AnyAsync(a => a.EmployeeId == dto.EmployeeId && a.Id != id))
            return Results.BadRequest("Employee already has an account");

        account.EmployeeId = dto.EmployeeId;
    }

    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("UpdateAccount")
.WithOpenApi()
.RequireAuthorization();

app.MapDelete("/api/accounts/{id}", async (int id, MasterContext db, ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Admin"))
        return Results.Forbid();

    var account = await db.Accounts.FindAsync(id);
    if (account == null)
        return Results.NotFound();

    db.Accounts.Remove(account);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeleteAccount")
.WithOpenApi()
.RequireAuthorization("AdminOnly");

app.MapGet("/api/devices", async (MasterContext db) =>
{
    var devices = await db.Devices
        .Select(d => new DeviceResponseDto
        {
            Id = d.Id,
            Name = d.Name,
            IsEnabled = d.IsEnabled,
            DeviceTypeName = d.DeviceType.Name
        })
        .ToListAsync();

    return Results.Ok(devices);
})
.WithName("GetDevices")
.WithOpenApi();

app.MapGet("/api/devices/{id}", async (int id, MasterContext db) =>
{
    var device = await db.Devices
        .Include(d => d.DeviceType)
        .FirstOrDefaultAsync(d => d.Id == id);

    if (device == null)
        return Results.NotFound();

    var currentUsage = await db.DeviceEmployees
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

    return Results.Ok(response);
})
.WithName("GetDeviceById")
.WithOpenApi();

app.MapPost("/api/devices", async (DeviceDto dto, MasterContext db) =>
{
    var context = new ValidationContext(dto);
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(dto, context, results, true))
        return Results.BadRequest(results);

    var deviceType = await db.DeviceTypes.FirstOrDefaultAsync(d => d.Name == dto.DeviceTypeName);
    if (deviceType == null)
        return Results.BadRequest($"Device type '{dto.DeviceTypeName}' is not found");

    var device = new Device
    {
        Name = dto.Name,
        IsEnabled = dto.IsEnabled,
        DeviceTypeId = deviceType.Id,
        AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties ?? new {})
    };

    db.Devices.Add(device);
    await db.SaveChangesAsync();

    return Results.Created($"/api/devices/{device.Id}", new { device.Id });
})
.WithName("CreateDevice")
.WithOpenApi();

app.MapPut("/api/devices/{id:int}", async (int id, DeviceDto dto, MasterContext db) =>
{
    var context = new ValidationContext(dto);
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(dto, context, results, true))
        return Results.BadRequest(results);

    var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id);
    if (device == null)
        return Results.NotFound($"Device with ID {id} not found.");

    var deviceType = await db.DeviceTypes.FirstOrDefaultAsync(dt => dt.Name == dto.DeviceTypeName);
    if (deviceType == null)
        return Results.BadRequest($"Device type '{dto.DeviceTypeName}' not found.");

    device.Name = dto.Name;
    device.IsEnabled = dto.IsEnabled;
    device.DeviceTypeId = deviceType.Id;
    device.AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties ?? new {});

    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("UpdateDevice")
.WithOpenApi();

app.MapDelete("/api/devices/{id:int}", async (int id, MasterContext db) =>
{
    var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id);
    if (device == null)
        return Results.NotFound($"Device with ID {id} not found.");
    
    db.Devices.Remove(device);
    await db.SaveChangesAsync();
    
    return Results.NoContent();
})
.WithName("DeleteDevice")
.WithOpenApi();

app.Run();