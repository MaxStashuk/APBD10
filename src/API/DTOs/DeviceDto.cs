using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace API.DTOs;

public class DeviceDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters")]
    public string Name { get; set; } = null!;

    public bool IsEnabled { get; set; } = true;

    [Required(ErrorMessage = "Device type name is required")]
    public string DeviceTypeName { get; set; } = null!;

    public object? AdditionalProperties { get; set; }
}