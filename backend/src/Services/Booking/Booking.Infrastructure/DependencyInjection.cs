using Booking.Application.Abstractions;
using TicketFlow.BuildingBlocks.Messaging;
using Booking.Infrastructure.Clients;
using Booking.Infrastructure.Messaging;
using Booking.Infrastructure.Persistence;
using Booking.Infrastructure.Persistence.Repositories;
using Booking.Infrastructure.Services;
using TicketFlow.BuildingBlocks.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Booking");

        services.AddDbContext<BookingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BookingDbContext>());
        services.AddScoped<IReservationRepository, ReservationRepository>();

        // Idempotency (Redis)
        services.AddDistributedMemoryCache(); // Geliştirme ortamı için bellek (Dockerda Real Redis'e bağlanmalıdır)
        services.AddTransient<IIdempotencyService, RedisIdempotencyService>();

        // Correlation ID Propagation
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdDelegatingHandler>();

        services.AddTicketFlowMessaging();

        // Catalog Servisine HTTP İstek atacak istemci (HttpClientFactory)
        var catalogBaseAddress = configuration["CatalogService:BaseAddress"] ?? "http://localhost:5002";
        services.AddHttpClient<ICatalogClient, CatalogHttpClient>(client =>
        {
            client.BaseAddress = new Uri(catalogBaseAddress);
        })
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

        // Kafka Background Consumer (Eventual Consistency Worker)
        services.AddHostedService<EventCancelledKafkaConsumer>();

        return services;
    }
}
