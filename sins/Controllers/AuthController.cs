using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using sins.Data;
using sins.Services;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace sins.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly DnsContext _context;

    public AuthController(AuthService authService, DnsContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            var username = request.Username ?? string.Empty;
            var password = request.Password ?? string.Empty;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, cancellationToken);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        ["error"] = Errors.InvalidGrant,
                        ["error_description"] = "Invalid username or password."
                    }),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var principal = BuildPrincipal(user, request.GetScopes());
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
            {
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        ["error"] = Errors.InvalidGrant,
                        ["error_description"] = "The refresh token is no longer valid."
                    }),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var subject = result.Principal.GetClaim(Claims.Subject);
            if (!int.TryParse(subject, out var userId))
            {
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        ["error"] = Errors.InvalidGrant,
                        ["error_description"] = "The refresh token subject is invalid."
                    }),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
            if (user == null)
            {
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        ["error"] = Errors.InvalidGrant,
                        ["error_description"] = "The user account is no longer available."
                    }),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var scopes = request.GetScopes().Any()
                ? request.GetScopes()
                : result.Principal.GetScopes();

            var principal = BuildPrincipal(user, scopes);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new
        {
            error = Errors.UnsupportedGrantType,
            error_description = "Only password and refresh_token grants are supported."
        });
    }

    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo")]
    public IActionResult UserInfo()
    {
        return Ok(new
        {
            sub = User.FindFirst(Claims.Subject)?.Value,
            preferred_username = User.FindFirst(Claims.PreferredUsername)?.Value,
            name = User.FindFirst(Claims.Name)?.Value,
            email = User.FindFirst(Claims.Email)?.Value,
            role = User.FindFirst(Claims.Role)?.Value
        });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            username = User.FindFirst(Claims.PreferredUsername)?.Value ?? User.Identity?.Name ?? string.Empty,
            email = User.FindFirst(Claims.Email)?.Value ?? string.Empty,
            role = User.FindFirst(Claims.Role)?.Value ?? "User"
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
        var users = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => new
            {
                id = u.Id,
                username = u.Username,
                email = u.Email,
                role = u.Role
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.IsActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ClaimsPrincipal BuildPrincipal(sins.Models.User user, IEnumerable<string> requestedScopes)
    {
        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            Claims.Name,
            Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.PreferredUsername, user.Username);
        identity.SetClaim(Claims.Name, user.Username);
        identity.SetClaim(Claims.Email, user.Email);
        identity.SetClaim(Claims.Role, user.Role);

        var principal = new ClaimsPrincipal(identity);
        var scopes = requestedScopes.ToHashSet(StringComparer.Ordinal);
        if (scopes.Count == 0)
        {
            scopes = new HashSet<string>(new[] { Scopes.OpenId, Scopes.Profile, Scopes.Email, "api" }, StringComparer.Ordinal);
        }
        principal.SetScopes(scopes);
        principal.SetResources("sins-api");

        principal.SetDestinations(static claim =>
        {
            return claim.Type switch
            {
                Claims.Subject or Claims.PreferredUsername or Claims.Name or Claims.Role
                    => new[] { Destinations.AccessToken, Destinations.IdentityToken },
                Claims.Email
                    => new[] { Destinations.AccessToken, Destinations.IdentityToken },
                _ => new[] { Destinations.AccessToken }
            };
        });

        return principal;
    }
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}
