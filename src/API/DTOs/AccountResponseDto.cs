namespace API.DTOs;

public class AccountResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public EmployeeInfoDto? Employee { get; set; }
} 