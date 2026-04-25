using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TicketFlow.BuildingBlocks.Messaging;

public abstract class KafkaConsumerBase<T>(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<KafkaConsumerBase<T>> logger)
    : BackgroundService where T : IntegrationEvent
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers =
            configuration["Kafka:BootstrapServers"]
            ?? configuration["Kafka__BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("Kafka:BootstrapServers")
            ?? Environment.GetEnvironmentVariable("Kafka__BootstrapServers")
            ?? "localhost:9092";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = $"{configuration["Kafka:ConsumerGroup"] ?? "ticketflow"}-{typeof(T).Name}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            SecurityProtocol = SecurityProtocol.SaslPlaintext,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = configuration["Kafka:SaslUsername"],
            SaslPassword = configuration["Kafka:SaslPassword"]
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(typeof(T).Name);

        logger.LogInformation("Started listening for {EventName} on Kafka", typeof(T).Name);

        // Ensure hosted service startup does not block web host startup.
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result?.Message?.Value == null) continue;

                var @event = JsonSerializer.Deserialize<T>(result.Message.Value);
                if (@event != null)
                {
                    using var scope = serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<T>>();
                    await handler.HandleAsync(@event, stoppingToken);
                }

                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Kafka message for {EventName}", typeof(T).Name);
                await Task.Delay(5000, stoppingToken);
            }
        }

        consumer.Close();
    }
}
