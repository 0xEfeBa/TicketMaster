using System.Reflection;
using Booking.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TicketFlow.BuildingBlocks.Messaging;
using TicketFlow.BuildingBlocks.Messaging.Events;
using Booking.Application.Features.Reservations.IntegrationEvents;

namespace Booking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()); });

        services.AddScoped<IIntegrationEventHandler<EventCancelledIntegrationEvent>, EventCancelledIntegrationEventHandler>();

        return services;
    }
}
