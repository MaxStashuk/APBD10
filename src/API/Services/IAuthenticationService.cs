using API.DTOs;
using API.Models;

namespace API.Services;

public interface IAuthenticationService
{
    string GenerateJwtToken(Account account, string roleName);
    string HashPassword(string password);
    Task<AuthResponseDto?> ValidateUserAsync(LoginDto loginDto);
    Task<bool> ValidateUserCredentialsAsync(string username, string password);
} 