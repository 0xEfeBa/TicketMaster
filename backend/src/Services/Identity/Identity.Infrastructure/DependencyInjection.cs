using Identity.Application.Abstractions;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Redis;
using Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        var redisCs = configuration[$"{RedisOptions.SectionName}:ConnectionString"] ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCs));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisCs;
            options.InstanceName = "Identity:";
        });

        services.AddSingleton<IRefreshTokenStore, RedisRefreshTokenStore>();
        services.AddSingleton<IAccessTokenBlacklist, RedisAccessTokenBlacklist>();
        services.AddSingleton<IAuthEndpointRateLimiter, RedisAuthEndpointRateLimiter>();
        services.AddScoped<IAuthTokenPairIssuer, AuthTokenPairIssuer>();
        services.AddScoped<ILogoutService, LogoutService>();

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Identity")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenIssuer, JwtTokenIssuer>();

        return services;
    }
}
