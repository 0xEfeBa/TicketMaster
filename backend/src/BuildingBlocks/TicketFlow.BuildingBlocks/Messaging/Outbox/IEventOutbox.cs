using System.Text.Json;

namespace TicketFlow.BuildingBlocks.Messaging.Outbox;

public interface IEventOutbox
{
    Task SaveAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent;
}

public class EfEventOutbox<TContext>(TContext dbContext) : IEventOutbox where TContext : Microsoft.EntityFrameworkCore.DbContext
{
    public async Task SaveAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent
    {
        var outboxMessage = new OutboxMessage
        {
            Id = @event.Id,
            OccurredOnUtc = @event.CreatedAt,
            Type = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName!,
            Content = JsonSerializer.Serialize(@event)
        };

        await dbContext.Set<OutboxMessage>().AddAsync(outboxMessage, ct);
    }
}
