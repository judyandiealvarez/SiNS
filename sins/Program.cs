using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using sins.Data;
using sins.Models;
using sins.Services;
using System.Data;
using static OpenIddict.Abstractions.OpenIddictConstants;

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
        Description = "OAuth2 access token using the Bearer scheme",
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
{
    options.UseNpgsql(connectionString ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured."));
    options.UseOpenIddict();
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<DnsContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.SetUserInfoEndpointUris("/connect/userinfo");
        options.SetRevocationEndpointUris("/connect/revocation");
        options.SetIntrospectionEndpointUris("/connect/introspect");

        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();
        options.AcceptAnonymousClients();

        options.RegisterScopes(
            Scopes.OpenId,
            Scopes.Profile,
            Scopes.Email,
            Scopes.OfflineAccess,
            "api");

        options.SetAccessTokenLifetime(TimeSpan.FromHours(24));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));
        options.DisableAccessTokenEncryption();

        options.AddEphemeralEncryptionKey()
            .AddEphemeralSigningKey();

        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough()
            .EnableUserInfoEndpointPassthrough()
            .EnableStatusCodePagesIntegration()
            .DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddAuthorization();

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddSingleton<IIngressHostResolver, KubernetesIngressHostResolver>();
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

// Vite dev server proxies http://127.0.0.1:5000 — HTTPS redirection breaks /api from the SPA in Development.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

// Serve static files
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

// Ensure database is created and initialize default configuration
try
{
    using (var scope = app.Services.CreateScope())
    {
    var context = scope.ServiceProvider.GetRequiredService<DnsContext>();
    context.Database.EnsureCreated();
    await EnsureOpenIddictSchemaAsync(context);

    // Create default admin user if no users exist
    if (!context.Users.Any())
        {
            var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
            await authService.CreateUserAsync("admin", "admin123", "admin@example.com", "Admin");
            Console.WriteLine("Default admin user created: admin / admin123");
        }

        // Register the SPA client used by the embedded OAuth2 server.
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        if (await applicationManager.FindByClientIdAsync("sins-spa") == null)
        {
            var spaClient = new OpenIddictApplicationDescriptor
            {
                ClientId = "sins-spa",
                ConsentType = ConsentTypes.Explicit,
                DisplayName = "SINS UI SPA",
                ClientType = ClientTypes.Public
            };

            spaClient.Permissions.Add(Permissions.Endpoints.Token);
            spaClient.Permissions.Add(Permissions.Endpoints.Revocation);
            spaClient.Permissions.Add(Permissions.GrantTypes.Password);
            spaClient.Permissions.Add(Permissions.GrantTypes.RefreshToken);
            spaClient.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OpenId);
            spaClient.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Profile);
            spaClient.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Email);
            spaClient.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OfflineAccess);
            spaClient.Permissions.Add(Permissions.Prefixes.Scope + "api");

            await applicationManager.CreateAsync(spaClient);
            Console.WriteLine("[Configuration] OpenIddict client registered: sins-spa");
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
                var haproxyEnv = Environment.GetEnvironmentVariable("HAPROXY");
                if (!string.IsNullOrWhiteSpace(haproxyEnv))
                {
                    await configService.SetValueAsync("Haproxy", haproxyEnv.Trim(), "System");
                    Console.WriteLine("[Configuration] Haproxy set from HAPROXY environment variable");
                }
                Console.WriteLine("Default configuration initialized");
            }
            else
            {
                // When config already exists, still allow HAPROXY env to override at startup (e.g. install/upgrade)
                var haproxyEnv = Environment.GetEnvironmentVariable("HAPROXY");
                if (!string.IsNullOrWhiteSpace(haproxyEnv))
                {
                    var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
                    await configService.SetValueAsync("Haproxy", haproxyEnv.Trim(), "System");
                    Console.WriteLine("[Configuration] Haproxy updated from HAPROXY environment variable");
                }
            }

            // Domain -> upstream mappings from env (e.g. k3s startup). Format: dev.net=10.11.4.17,test.net=10.11.3.17
            var domainMappingsEnv = Environment.GetEnvironmentVariable("DOMAIN_UPSTREAM_MAPPINGS");
            if (!string.IsNullOrWhiteSpace(domainMappingsEnv))
            {
                foreach (var pair in domainMappingsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var eq = pair.IndexOf('=');
                    if (eq <= 0 || eq == pair.Length - 1) continue;
                    var domain = pair.Substring(0, eq).Trim().TrimEnd('.').ToLowerInvariant();
                    var upstream = pair.Substring(eq + 1).Trim();
                    if (string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(upstream)) continue;

                    var existing = await context.DomainUpstreamMappings.FirstOrDefaultAsync(m => m.Domain == domain);
                    if (existing != null)
                    {
                        existing.UpstreamServer = upstream;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        context.DomainUpstreamMappings.Add(new DomainUpstreamMapping
                        {
                            Domain = domain,
                            UpstreamServer = upstream,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
                await context.SaveChangesAsync();
                Console.WriteLine("[Configuration] Domain upstream mappings updated from DOMAIN_UPSTREAM_MAPPINGS");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not initialize configuration: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Failed to initialize database: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Console.WriteLine("Application will continue but may not function correctly without database access.");
}

app.Run();

static async Task EnsureOpenIddictSchemaAsync(DnsContext context)
{
    // EnsureCreated() does not add new tables when the DB already has any table.
    // Existing installs need this bootstrap when OpenIddict is introduced later.
    var connection = context.Database.GetDbConnection();
    if (connection.State != ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    await using var checkCmd = connection.CreateCommand();
    checkCmd.CommandText = "SELECT to_regclass('public.\"OpenIddictApplications\"') IS NOT NULL;";
    var existsObj = await checkCmd.ExecuteScalarAsync();
    var hasOpenIddictSchema = existsObj is bool b && b;
    if (hasOpenIddictSchema)
    {
        return;
    }

    await context.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE IF NOT EXISTS "OpenIddictApplications" (
            "Id" text NOT NULL,
            "ApplicationType" character varying(50),
            "ClientId" character varying(100),
            "ClientSecret" text,
            "ClientType" character varying(50),
            "ConcurrencyToken" character varying(50),
            "ConsentType" character varying(50),
            "DisplayName" text,
            "DisplayNames" text,
            "JsonWebKeySet" text,
            "Permissions" text,
            "PostLogoutRedirectUris" text,
            "Properties" text,
            "RedirectUris" text,
            "Requirements" text,
            "Settings" text,
            CONSTRAINT "PK_OpenIddictApplications" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS "OpenIddictScopes" (
            "Id" text NOT NULL,
            "ConcurrencyToken" character varying(50),
            "Description" text,
            "Descriptions" text,
            "DisplayName" text,
            "DisplayNames" text,
            "Name" character varying(200),
            "Properties" text,
            "Resources" text,
            CONSTRAINT "PK_OpenIddictScopes" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS "OpenIddictAuthorizations" (
            "Id" text NOT NULL,
            "ApplicationId" text,
            "ConcurrencyToken" character varying(50),
            "CreationDate" timestamp with time zone,
            "Properties" text,
            "Scopes" text,
            "Status" character varying(50),
            "Subject" character varying(400),
            "Type" character varying(50),
            CONSTRAINT "PK_OpenIddictAuthorizations" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_OpenIddictAuthorizations_OpenIddictApplications_ApplicationId"
                FOREIGN KEY ("ApplicationId") REFERENCES "OpenIddictApplications" ("Id")
        );

        CREATE TABLE IF NOT EXISTS "OpenIddictTokens" (
            "Id" text NOT NULL,
            "ApplicationId" text,
            "AuthorizationId" text,
            "ConcurrencyToken" character varying(50),
            "CreationDate" timestamp with time zone,
            "ExpirationDate" timestamp with time zone,
            "Payload" text,
            "Properties" text,
            "RedemptionDate" timestamp with time zone,
            "ReferenceId" character varying(100),
            "Status" character varying(50),
            "Subject" character varying(400),
            "Type" character varying(150),
            CONSTRAINT "PK_OpenIddictTokens" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_OpenIddictTokens_OpenIddictApplications_ApplicationId"
                FOREIGN KEY ("ApplicationId") REFERENCES "OpenIddictApplications" ("Id"),
            CONSTRAINT "FK_OpenIddictTokens_OpenIddictAuthorizations_AuthorizationId"
                FOREIGN KEY ("AuthorizationId") REFERENCES "OpenIddictAuthorizations" ("Id")
        );

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_OpenIddictApplications_ClientId" ON "OpenIddictApplications" ("ClientId");
        CREATE INDEX IF NOT EXISTS "IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type"
            ON "OpenIddictAuthorizations" ("ApplicationId", "Status", "Subject", "Type");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_OpenIddictScopes_Name" ON "OpenIddictScopes" ("Name");
        CREATE INDEX IF NOT EXISTS "IX_OpenIddictTokens_ApplicationId_Status_Subject_Type"
            ON "OpenIddictTokens" ("ApplicationId", "Status", "Subject", "Type");
        CREATE INDEX IF NOT EXISTS "IX_OpenIddictTokens_AuthorizationId" ON "OpenIddictTokens" ("AuthorizationId");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_OpenIddictTokens_ReferenceId" ON "OpenIddictTokens" ("ReferenceId");
        """
    );

    Console.WriteLine("[Configuration] OpenIddict schema bootstrap completed.");
}