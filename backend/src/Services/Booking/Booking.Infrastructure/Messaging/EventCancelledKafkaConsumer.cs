using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TicketFlow.BuildingBlocks.Messaging;
using TicketFlow.BuildingBlocks.Messaging.Events;

namespace Booking.Infrastructure.Messaging;

public class EventCancelledKafkaConsumer(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<EventCancelledKafkaConsumer> logger) 
    : KafkaConsumerBase<EventCancelledIntegrationEvent>(serviceProvider, configuration, logger);
