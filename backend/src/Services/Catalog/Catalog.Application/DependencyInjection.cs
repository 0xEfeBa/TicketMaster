using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Catalog.Application.Behaviors;

namespace Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            // Register behaviors
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            // Add validation behavior here if created...
        });

        return services;
    }
}
