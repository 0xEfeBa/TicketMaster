using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TicketFlow.BuildingBlocks.Messaging.Outbox;

public class OutboxProcessor<TContext>(
    IServiceProvider serviceProvider,
    IEventBus eventBus,
    ILogger<OutboxProcessor<TContext>> logger) 
    : BackgroundService where TContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Processor for {ContextName} started", typeof(TContext).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

                var messages = await dbContext.Set<OutboxMessage>()
                    .Where(m => m.ProcessedOnUtc == null)
                    .OrderBy(m => m.OccurredOnUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                if (messages.Count == 0)
                {
                    await Task.Delay(2000, stoppingToken);
                    continue;
                }

                foreach (var message in messages)
                {
                    try
                    {
                        var eventType = Type.GetType(message.Type);
                        if (eventType == null)
                        {
                            logger.LogError("Could not find type {TypeName}", message.Type);
                            message.Error = $"Type {message.Type} not found";
                        }
                        else
                        {
                            var @event = JsonSerializer.Deserialize(message.Content, eventType) as IntegrationEvent;
                            if (@event != null)
                            {
                                await eventBus.PublishAsync(@event, stoppingToken);
                                message.ProcessedOnUtc = DateTime.UtcNow;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);
                        message.Error = ex.Message;
                    }
                }

                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox Processor encountered an error");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
