using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Infrastructure.Caching;
using PhanMemKeToan.Infrastructure.Persistence;
using PhanMemKeToan.Infrastructure.Persistence.Interceptors;
using PhanMemKeToan.Infrastructure.Services;

namespace PhanMemKeToan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ITenantContext, TenantContext>();

        services.AddScoped<AuditableEntityInterceptor>();

        // Master DB context (fixed connection string)
        services.AddDbContext<MasterDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("MasterConnection")
                    ?? configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(MasterDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IMasterDbContext>(sp =>
            sp.GetRequiredService<MasterDbContext>());

        // Tenant DB context (per-tenant connection)
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // DataProtection for connection string encryption
        services.AddDataProtection()
            .SetApplicationName("PhanMemKeToan");

        // Tenant DB context factory
        services.AddScoped<TenantDbContextFactory>();

        // Redis distributed cache
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "PMKeToan:";
            });
        }

        // Service registrations
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITempTokenService, TempTokenService>();
        services.AddScoped<ITokenBlacklistService, RedisTokenBlacklistService>();
        services.AddScoped<IAccountCacheService, AccountCacheService>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IConnectionStringEncryptor, DataProtectionEncryptor>();
        services.AddScoped<ITenantConnectionResolver, TenantConnectionResolver>();

        services.AddHostedService<ExpiredTokenCleanupService>();

        return services;
    }
}

