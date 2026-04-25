namespace TicketFlow.BuildingBlocks.Messaging.Events;

public record EventCancelledIntegrationEvent(Guid EventId) : IntegrationEvent;
