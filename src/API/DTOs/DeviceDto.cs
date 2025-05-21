using System.Security.Cryptography.X509Certificates;

namespace API.DTOs;

public class DeviceDto
{
    public string Name { get; set; }
    public string TypeName { get; set; }
    public bool IsEnabled { get; set; }
    public object AdditionalProperties { get; set; }
}