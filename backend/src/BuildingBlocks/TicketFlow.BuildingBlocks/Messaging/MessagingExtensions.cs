using Microsoft.Extensions.DependencyInjection;
using TicketFlow.BuildingBlocks.Messaging;

namespace TicketFlow.BuildingBlocks.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddTicketFlowMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, KafkaEventBus>();
        return services;
    }
}
