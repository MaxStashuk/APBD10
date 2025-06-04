using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using API.DTOs;
using Microsoft.EntityFrameworkCore;
using API.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                       throw new InvalidOperationException("No connection string found");

// builder.Services.AddDbContext<MasterContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

/*
app.MapGet("/api/devices", async (MasterContext db) =>
{
    var devices = await db.Devices
        .Select(d => new
        {
            d.Id,
            d.Name
        })
        .ToListAsync();

    return Results.Ok(devices);
});


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
    
    var result = new
    {
        device.Name,
        DeviceTypeName = device.DeviceType?.Name,
        device.IsEnabled,
        AdditionalProperties = JsonSerializer.Deserialize<object>(device.AdditionalProperties),
        CurrentEmployee = currentUsage == null ? null : new
        {
            Id = currentUsage.Employee.Id,
            Name = $"{currentUsage.Employee.Person.FirstName} {currentUsage.Employee.Person.LastName}"
        }
    };
    return Results.Ok(result);
});

app.MapPost("/api/devices", async (DeviceDto dto, MasterContext db) =>
{
    var context = new ValidationContext(dto);
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(dto, context, results, true))
    {
        return Results.BadRequest(results);
    }

    var deviceType = await db.DeviceTypes.FirstOrDefaultAsync(d => d.Name == dto.TypeName);
    if (deviceType == null)
        return Results.BadRequest($"Device type '{dto.TypeName}' is not found");

    var device = new Device
    {
        Name = dto.Name,
        IsEnabled = dto.IsEnabled,
        DeviceTypeId = deviceType.Id,
        AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties),
    };

    db.Devices.Add(device);
    await db.SaveChangesAsync();
    return Results.Created($"/api/devices/{device.Id}", new { device.Id });
});



app.MapPut("/api/devices/{id:int}", async (int id, DeviceDto dto, MasterContext db) =>
{
    var context = new ValidationContext(dto);
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(dto, context, results, true))
    {
        return Results.BadRequest(results);
    }

    var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id);
    if (device == null)
        return Results.NotFound($"Device with ID {id} not found.");

    var deviceType = await db.DeviceTypes.FirstOrDefaultAsync(dt => dt.Name == dto.TypeName);
    if (deviceType == null)
        return Results.BadRequest($"Device type '{dto.TypeName}' not found.");

    device.Name = dto.Name;
    device.IsEnabled = dto.IsEnabled;
    device.DeviceTypeId = deviceType.Id;
    device.AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties);

    await db.SaveChangesAsync();

    return Results.NoContent();
});


app.MapDelete("/api/devices/{id:int}", async (int id, MasterContext db) =>
{
    var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id);
    if (device == null)
        return Results.NotFound($"Device with ID {id} not found.");
    
    db.Devices.Remove(device);
    await db.SaveChangesAsync();
    
    return Results.NoContent();
});


app.MapGet("/api/employees", async (MasterContext db) =>
{
    var employees = await db.Employees
        .Include(e => e.Person)
        .Select(e => new
        {
            e.Id,
            FullName = $"{e.Person.FirstName} " +
                       $"{(string.IsNullOrEmpty(e.Person.MiddleName) ? "" : e.Person.MiddleName + " ")}" +
                       $"{e.Person.LastName}"
        })
        .ToListAsync();

    return Results.Ok(employees);
});

app.MapGet("/api/employees/{id:int}", async (int id, MasterContext db) =>
{
    var employee = await db.Employees
        .Include(e => e.Person)
        .Include(e => e.Position)
        .Where(e => e.Id == id)
        .Select(e => new
        {
            Id = e.Id,
            Person = new
            {
                e.Person.FirstName,
                e.Person.MiddleName,
                e.Person.LastName,
                e.Person.PassportNumber,
                e.Person.Email,
                e.Person.PhoneNumber
            },
            Salary = e.Salary,
            Position = new
            {
                Id = e.Position.Id,
                Name = e.Position.Name
            },
            HireDate = e.HireDate
        })
        .FirstOrDefaultAsync();

    if (employee == null)
        return Results.NotFound($"Employee with ID {id} not found.");

    return Results.Ok(employee);
});
*/

app.Run();