using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using PhanMemKeToan.Api.Infrastructure;
using PhanMemKeToan.Api.Services;
using PhanMemKeToan.Application;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Infrastructure;
using Serilog;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));

    // Application & Infrastructure services
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Current user service
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Global exception handler
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Controllers
    builder.Services.AddControllers();

    // JWT Bearer authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // Keep JWT claim names as-is (prevent "tid" → MS claim type mapping)

        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var publicKeyPem = jwtSettings["PublicKeyPemBase64"] ?? throw new InvalidOperationException("JwtSettings:PublicKeyPemBase64 required");
        var publicKeyBytes = Convert.FromBase64String(publicKeyPem);
        var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportFromPem(System.Text.Encoding.UTF8.GetString(publicKeyBytes));
        var publicKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa);

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = publicKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = new[] { Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha256 },
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                if (!string.IsNullOrEmpty(jti))
                {
                    var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                    if (await blacklist.IsBlacklistedAsync(jti))
                        context.Fail("Token has been revoked.");
                }
            }
        };
    });

    builder.Services.AddSingleton<IAuthorizationHandler, PhanMemKeToan.Api.Authorization.PermissionAuthorizationHandler>();
    builder.Services.AddAuthorization(options =>
    {
        // SYS module permissions
        var permissions = new[]
        {
            "SYS.Users.View", "SYS.Users.Manage",
            "SYS.Roles.View", "SYS.Roles.Manage",
            "SYS.Permissions.View", "SYS.Permissions.Manage",
            // DI module — Account Tree
            "DI.Accounts.View", "DI.Accounts.Manage", "DI.Accounts.Import"
        };
        foreach (var perm in permissions)
        {
            options.AddPolicy(perm, policy =>
                policy.Requirements.Add(new PhanMemKeToan.Api.Authorization.PermissionRequirement(perm)));
        }
    });

    // Rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("login", limiterOptions =>
        {
            limiterOptions.PermitLimit = 20;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // CORS
    var corsOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? [];
    if (corsOrigins.Length == 0 && !builder.Environment.IsDevelopment())
        throw new InvalidOperationException("CorsSettings:AllowedOrigins must be configured in production. Use user-secrets or environment variables.");

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultCors", policy =>
        {
            if (corsOrigins.Length > 0)
                policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            else
                policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials(); // Development only — never in production
        });
    });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty, name: "postgres")
        .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? string.Empty, name: "redis");

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("DefaultCors");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseMiddleware<PhanMemKeToan.Infrastructure.Middleware.TenantMiddleware>();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

