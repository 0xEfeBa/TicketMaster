using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TicketFlow.BuildingBlocks.Messaging.Outbox;

public static class OutboxExtensions
{
    public static IServiceCollection AddTicketFlowOutbox<TContext>(this IServiceCollection services) 
        where TContext : DbContext
    {
        services.AddScoped<IEventOutbox, EfEventOutbox<TContext>>();
        services.AddHostedService<OutboxProcessor<TContext>>();
        return services;
    }
}
