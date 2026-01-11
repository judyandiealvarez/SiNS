using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using sins.Data;
using sins.Models;
using Dapper;

namespace sins.Services;

public class AuthService
{
    private readonly IConfiguration _configuration;
    private readonly IDatabaseService _databaseService;

    public AuthService(IConfiguration configuration, IDatabaseService databaseService)
    {
        _configuration = configuration;
        _databaseService = databaseService;
    }

    public async Task<string?> AuthenticateAsync(string username, string password)
    {
        using var connection = _databaseService.GetConnection();
        var user = await connection.QueryFirstOrDefaultAsync<User>(@"
            SELECT * FROM ""Users""
            WHERE ""Username"" = @Username AND ""IsActive"" = true
            LIMIT 1
        ", new { Username = username });

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return GenerateJwtToken(user);
    }

    public async Task<bool> CreateUserAsync(string username, string password, string email, string role = "User")
    {
        using var connection = _databaseService.GetConnection();
        var exists = await connection.QueryFirstOrDefaultAsync<int?>(@"
            SELECT ""Id"" FROM ""Users""
            WHERE ""Username"" = @Username OR ""Email"" = @Email
            LIMIT 1
        ", new { Username = username, Email = email });

        if (exists != null)
        {
            return false;
        }

        var createdAt = DateTime.UtcNow;
        await connection.ExecuteAsync(@"
            INSERT INTO ""Users"" (""Username"", ""PasswordHash"", ""Email"", ""Role"", ""CreatedAt"", ""IsActive"")
            VALUES (@Username, @PasswordHash, @Email, @Role, @CreatedAt, @IsActive)
        ", new
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Email = email,
            Role = role,
            CreatedAt = createdAt,
            IsActive = true
        });

        return true;
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "your-secret-key-here"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
