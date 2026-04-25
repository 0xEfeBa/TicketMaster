using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TicketFlow.BuildingBlocks.Messaging;

public class KafkaEventBus : IEventBus, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventBus> _logger;

    public KafkaEventBus(IConfiguration configuration, ILogger<KafkaEventBus> logger)
    {
        _logger = logger;

        var bootstrapServers =
            configuration["Kafka:BootstrapServers"]
            ?? configuration["Kafka__BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("Kafka:BootstrapServers")
            ?? Environment.GetEnvironmentVariable("Kafka__BootstrapServers")
            ?? "localhost:9092";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslPlaintext,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = configuration["Kafka:SaslUsername"],
            SaslPassword = configuration["Kafka:SaslPassword"]
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent
    {
        var topic = typeof(T).Name;
        var message = JsonSerializer.Serialize(@event);

        try
        {
            _logger.LogInformation("Publishing event {EventId} to topic {Topic}", @event.Id, topic);
            await _producer.ProduceAsync(topic, new Message<string, string> 
            { 
                Key = @event.Id.ToString(), 
                Value = message 
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventId} to topic {Topic}", @event.Id, topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
