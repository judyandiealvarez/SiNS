using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sins.Data;
using sins.Services;
using BCrypt.Net;
using Dapper;

namespace sins.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IDatabaseService _databaseService;

    public AuthController(AuthService authService, IDatabaseService databaseService)
    {
        _authService = authService;
        _databaseService = databaseService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Username and password are required" });
        }

        using var connection = _databaseService.GetConnection();
        var user = await connection.QueryFirstOrDefaultAsync<sins.Models.User>(@"
            SELECT * FROM ""Users""
            WHERE ""Username"" = @Username AND ""IsActive"" = true
            LIMIT 1
        ", new { Username = request.Username });

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var token = await _authService.AuthenticateAsync(request.Username, request.Password);

        return Ok(new
        {
            token,
            user = new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                role = user.Role
            }
        });
    }

    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { message = "Username, password, and email are required" });
        }

        var success = await _authService.CreateUserAsync(request.Username, request.Password, request.Email, request.Role);

        if (!success)
        {
            return BadRequest(new { message = "Username or email already exists" });
        }

        return Ok(new { message = "User created successfully" });
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers()
    {
        using var connection = _databaseService.GetConnection();
        var users = await connection.QueryAsync(@"
            SELECT ""Id"", ""Username"", ""Email"", ""Role"", ""CreatedAt""
            FROM ""Users""
            WHERE ""IsActive"" = true
            ORDER BY ""Username""
        ");

        return Ok(users);
    }

    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        using var connection = _databaseService.GetConnection();
        var exists = await connection.QueryFirstOrDefaultAsync<int?>(@"
            SELECT ""Id"" FROM ""Users""
            WHERE ""Id"" = @Id
            LIMIT 1
        ", new { Id = id });

        if (exists == null)
        {
            return NotFound();
        }

        await connection.ExecuteAsync(@"
            UPDATE ""Users""
            SET ""IsActive"" = false
            WHERE ""Id"" = @Id
        ", new { Id = id });

        return NoContent();
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}
