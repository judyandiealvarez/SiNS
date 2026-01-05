using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using sins.Data;
using sins.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from /etc/sins/appsettings.json if it exists (for deb package installation)
// Insert at position 1 (after appsettings.json but before environment variables)
// This ensures environment variables can override values from this file
if (File.Exists("/etc/sins/appsettings.json"))
{
    var jsonSource = new Microsoft.Extensions.Configuration.Json.JsonConfigurationSource
    {
        Path = "/etc/sins/appsettings.json",
        Optional = true,
        ReloadOnChange = true
    };
    jsonSource.ResolveFileProvider();
    // Insert after appsettings.json (index 0) but before environment variables
    // Environment variables are typically at index 2-3, so insert at 1
    if (builder.Configuration.Sources.Count > 1)
    {
        builder.Configuration.Sources.Insert(1, jsonSource);
    }
    else
    {
        builder.Configuration.Sources.Insert(0, jsonSource);
    }
}

// Get connection string - ALWAYS check environment variable first (highest priority)
// This ensures env vars override even if JSON file is loaded
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Log connection string for debugging (masking password)
if (!string.IsNullOrEmpty(connectionString))
{
    // Mask password in log output
    var maskedConnectionString = connectionString;
    var passwordMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Password=([^;]+)");
    if (passwordMatch.Success)
    {
        maskedConnectionString = connectionString.Replace($"Password={passwordMatch.Groups[1].Value}", "Password=***");
    }
    Console.WriteLine($"[Configuration] Using connection string: {maskedConnectionString}");
    
    // Check if it contains a hardcoded IP (which shouldn't be used in containerized environments)
    if (System.Text.RegularExpressions.Regex.IsMatch(connectionString, @"Host=\d+\.\d+\.\d+\.\d+"))
    {
        Console.WriteLine("[Configuration] WARNING: Connection string contains hardcoded IP address. Consider using service name or environment variable.");
    }
}
else
{
    Console.WriteLine("[Configuration] ERROR: No connection string found! Set ConnectionStrings__DefaultConnection environment variable.");
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DNS Server API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Entity Framework
builder.Services.AddDbContext<DnsContext>(options =>
    options.UseNpgsql(connectionString ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.")));

// Configure Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "your-secret-key-here"))
        };
    });

builder.Services.AddAuthorization();

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddHostedService<DnsServer>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Serve static files
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created and initialize default configuration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DnsContext>();
    context.Database.EnsureCreated();

    // Create default admin user if no users exist
    if (!context.Users.Any())
    {
        var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
        await authService.CreateUserAsync("admin", "admin123", "admin@example.com", "Admin");
        Console.WriteLine("Default admin user created: admin / admin123");
    }

    // Initialize default configuration
    try
    {
        // Check if ServerConfigs table exists and has any records
        var hasConfigs = await context.ServerConfigs.AnyAsync();

        if (!hasConfigs)
        {
            var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();

            await configService.SetValueAsync("CacheTimeoutMinutes", "60", "System");
            await configService.SetValueAsync("UdpPort", "53", "System");
            await configService.SetValueAsync("TcpPort", "53", "System");
            await configService.SetValueAsync("UpstreamServers", "8.8.8.8,1.1.1.1,8.8.4.4", "System");
            Console.WriteLine("Default configuration initialized");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not initialize configuration: {ex.Message}");
    }
}

app.Run();