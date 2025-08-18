using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using sins.Data;
using sins.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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