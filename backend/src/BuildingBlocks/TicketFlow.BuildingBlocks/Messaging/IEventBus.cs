namespace TicketFlow.BuildingBlocks.Messaging;

public record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent;
}

public interface IIntegrationEventHandler<in T> where T : IntegrationEvent
{
    Task HandleAsync(T @event, CancellationToken ct = default);
}
