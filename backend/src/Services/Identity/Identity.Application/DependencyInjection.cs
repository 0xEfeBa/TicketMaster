using Identity.Application.Admin;
using Identity.Application.Auth;
using Identity.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<AssignRoleHandler>();
        services.AddScoped<RefreshTokenHandler>();
        return services;
    }
}