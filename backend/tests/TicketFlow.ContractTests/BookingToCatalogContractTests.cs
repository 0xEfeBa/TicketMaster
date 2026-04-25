using System.Net;
using PactNet;
using Xunit;

namespace TicketFlow.ContractTests;

/// <summary>
/// Catalog GET /api/v1/events/{id} yanıtı, GetEventById EventDetailDto ile aynı alan isimleri ve enum (status) değerleriyle hizalanır.
/// </summary>
public class BookingToCatalogContractTests
{
    private readonly IPactBuilderV4 _pactBuilder;

    public BookingToCatalogContractTests()
    {
        var config = new PactConfig();
        var pact = Pact.V4("BookingService", "CatalogService", config);
        _pactBuilder = pact.WithHttpInteractions();
    }

    [Fact]
    public async Task GetEvent_WhenEventExists_ReturnsOk()
    {
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var ticketTypeId = Guid.NewGuid();
        var organizerUserId = Guid.NewGuid();
        var startsAt = new DateTimeOffset(2026, 5, 1, 20, 0, 0, TimeSpan.Zero);
        var endsAt = new DateTimeOffset(2026, 5, 1, 23, 0, 0, TimeSpan.Zero);

        _pactBuilder
            .UponReceiving("A GET request to retrieve an event")
                .Given("An event exists")
                .WithRequest(HttpMethod.Get, $"/api/v1/events/{eventId}")
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    id = eventId,
                    title = "Müzik Festivali",
                    description = "Örnek açıklama",
                    venue = "İstanbul",
                    imageUrl = (string?)null,
                    status = 1,
                    organizerUserId,
                    sessions = new[]
                    {
                        new { id = sessionId, startsAt, endsAt = (DateTimeOffset?)endsAt }
                    },
                    ticketTypes = new[]
                    {
                        new { id = ticketTypeId, name = "Genel", priceAmount = 450.00m, totalQuantity = 1000 }
                    }
                });

        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            var response = await client.GetAsync($"/api/v1/events/{eventId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }
}
