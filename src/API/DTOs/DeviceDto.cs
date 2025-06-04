using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace API.DTOs;

public class DeviceDto
{
    [Required]
    public string Name { get; set; }
    [Required]
    public string TypeName { get; set; }
    
    public bool IsEnabled { get; set; }
    [Required]
    public string AdditionalProperties { get; set; }
}