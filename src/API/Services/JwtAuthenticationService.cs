using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.DTOs;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace API.Services;

public class JwtAuthenticationService : IAuthenticationService
{
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly MasterContext _context;

    public JwtAuthenticationService(
        IConfiguration configuration,
        MasterContext context)
    {
        _jwtKey = configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key");
        _jwtIssuer = configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer");
        _jwtAudience = configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience");
        _context = context;
    }

    public string GenerateJwtToken(Account account, string roleName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, account.Username),
            new(ClaimTypes.Role, roleName),
            new("AccountId", account.Id.ToString()),
            new("EmployeeId", account.EmployeeId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    public async Task<AuthResponseDto?> ValidateUserAsync(LoginDto loginDto)
    {
        var account = await _context.Accounts
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Username == loginDto.Username);

        if (account == null || account.Password != HashPassword(loginDto.Password))
            return null;

        var token = GenerateJwtToken(account, account.Role.Name ?? "User");
        return new AuthResponseDto { Token = token };
    }

    public async Task<bool> ValidateUserCredentialsAsync(string username, string password)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Username == username);

        return account != null && account.Password == HashPassword(password);
    }
} 