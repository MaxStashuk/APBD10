using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class AccountRegistrationDto
{
    [Required(ErrorMessage = "Username is required")]
    [RegularExpression("^[a-zA-Z][a-zA-Z0-9]*$", ErrorMessage = "Username must start with a letter and contain only letters and numbers")]
    [StringLength(150, ErrorMessage = "Username cannot exceed 150 characters")]
    public string Username { get; set; } = null!;
    
    [Required(ErrorMessage = "Password is required")]
    [StringLength(150, MinimumLength = 12, ErrorMessage = "Password must be between 12 and 150 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{12,}$", 
        ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one number, and one special character")]
    public string Password { get; set; } = null!;
    
    [Required(ErrorMessage = "Employee ID is required")]
    public int EmployeeId { get; set; }
} 