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
    private readonly KeycloakTokenService _keycloakTokenService;
    private readonly IConfiguration _configuration;

    public AuthController(
        AuthService authService,
        DnsContext context,
        KeycloakTokenService keycloakTokenService,
        IConfiguration configuration)
    {
        _authService = authService;
        _context = context;
        _keycloakTokenService = keycloakTokenService;
        _configuration = configuration;
    }

    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        if (UseKeycloak())
        {
            return BadRequest(new { message = "Embedded OAuth2 is disabled while Auth:Provider is Keycloak." });
        }

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

    [Authorize]
    [HttpGet("~/connect/userinfo")]
    public IActionResult UserInfo()
    {
        return Ok(new
        {
            sub = GetFirstClaim(User, Claims.Subject, ClaimTypes.NameIdentifier, "sub"),
            preferred_username = GetFirstClaim(User, Claims.PreferredUsername, "preferred_username", ClaimTypes.Name),
            name = GetFirstClaim(User, Claims.Name, "name", ClaimTypes.Name),
            email = GetFirstClaim(User, Claims.Email, "email", ClaimTypes.Email),
            role = GetFirstClaim(User, Claims.Role, "role", ClaimTypes.Role)
        });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var username = GetFirstClaim(User, Claims.PreferredUsername, "preferred_username", ClaimTypes.Name)
            ?? User.Identity?.Name
            ?? string.Empty;
        var email = GetFirstClaim(User, Claims.Email, "email", ClaimTypes.Email) ?? string.Empty;
        var role = ResolveRole(User);

        return Ok(new
        {
            username,
            email,
            role
        });
    }

    [HttpGet("provider")]
    [AllowAnonymous]
    public IActionResult Provider()
    {
        var provider = UseKeycloak() ? "Keycloak" : "Embedded";
        return Ok(new { provider });
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

    [HttpPost("keycloak/login")]
    [AllowAnonymous]
    public async Task<IActionResult> KeycloakLogin([FromBody] KeycloakLoginRequest request, CancellationToken cancellationToken)
    {
        if (!UseKeycloak())
        {
            return BadRequest(new { message = "Keycloak auth is not enabled." });
        }
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        var result = await _keycloakTokenService.PasswordGrantAsync(request.Username, request.Password, cancellationToken);
        if (!result.Ok)
        {
            return StatusCode(result.StatusCode, ParseJsonOrText(result.Body, result.Error));
        }
        return Content(result.Body, "application/json");
    }

    [HttpPost("keycloak/refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> KeycloakRefresh([FromBody] KeycloakRefreshRequest request, CancellationToken cancellationToken)
    {
        if (!UseKeycloak())
        {
            return BadRequest(new { message = "Keycloak auth is not enabled." });
        }
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        var result = await _keycloakTokenService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (!result.Ok)
        {
            return StatusCode(result.StatusCode, ParseJsonOrText(result.Body, result.Error));
        }
        return Content(result.Body, "application/json");
    }

    [HttpPost("keycloak/logout")]
    [AllowAnonymous]
    public async Task<IActionResult> KeycloakLogout([FromBody] KeycloakLogoutRequest request, CancellationToken cancellationToken)
    {
        if (!UseKeycloak())
        {
            return BadRequest(new { message = "Keycloak auth is not enabled." });
        }
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required." });
        }

        var result = await _keycloakTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        if (!result.Ok)
        {
            return StatusCode(result.StatusCode, ParseJsonOrText(result.Body, result.Error));
        }
        return Content(result.Body, "application/json");
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

    private bool UseKeycloak() =>
        string.Equals(_configuration["Auth:Provider"], "Keycloak", StringComparison.OrdinalIgnoreCase);

    private static string? GetFirstClaim(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string ResolveRole(ClaimsPrincipal principal)
    {
        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(r => r.Value)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (roles.Count == 0)
        {
            var fallback = GetFirstClaim(principal, Claims.Role, "role");
            return string.IsNullOrWhiteSpace(fallback) ? "User" : fallback;
        }

        if (roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)))
        {
            return "Admin";
        }
        if (roles.Any(r => string.Equals(r, "User", StringComparison.OrdinalIgnoreCase)))
        {
            return "User";
        }

        var ignoredRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "offline_access",
            "uma_authorization"
        };
        var meaningfulRole = roles.FirstOrDefault(r => !ignoredRoles.Contains(r) && !r.StartsWith("default-roles-", StringComparison.OrdinalIgnoreCase));
        return meaningfulRole ?? roles[0];
    }

    private static object ParseJsonOrText(string body, string? fallbackError)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<object>(body);
                if (parsed != null)
                {
                    return parsed;
                }
            }
            catch
            {
                // Return raw response below.
            }
            return new { message = body };
        }

        return new { message = fallbackError ?? "Auth request failed." };
    }
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

public class KeycloakLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class KeycloakRefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class KeycloakLogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
