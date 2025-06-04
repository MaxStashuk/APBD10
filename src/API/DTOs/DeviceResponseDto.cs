namespace API.DTOs;

public class DeviceResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? DeviceTypeName { get; set; }
    public bool IsEnabled { get; set; }
    public object? AdditionalProperties { get; set; }
    public CurrentEmployeeDto? CurrentEmployee { get; set; }
}

public class CurrentEmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
} 