using Catalog.Application.Abstractions;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.BuildingBlocks.Messaging;
using TicketFlow.BuildingBlocks.Messaging.Outbox;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Catalog");

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddTicketFlowMessaging();
        services.AddTicketFlowOutbox<CatalogDbContext>();

        // Redis setup
        var redisConnStr = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnStr))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnStr;
                options.InstanceName = "Catalog:";
            });
        }
        else
        {
            // Dev environment fallback
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
