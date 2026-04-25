using System.Net.Http.Headers;
using Booking.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Booking.Infrastructure.Clients;

public class CatalogHttpClient : ICatalogClient
{
    private readonly HttpClient _client;
    private readonly ILogger<CatalogHttpClient> _logger;

    public CatalogHttpClient(HttpClient client, ILogger<CatalogHttpClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ValidateTicketTypeResult> ValidateTicketTypeAsync(Guid eventId, Guid ticketTypeId, string bearerToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/events/{eventId}");
            if (!string.IsNullOrEmpty(bearerToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken); // Kullanıcının Auth Tokenı aynen iletiliyor (Forwarding)

            var response = await _client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new ValidateTicketTypeResult(false, 0, $"Katalog servisine ulaşılamadı (Status: {response.StatusCode}).");

            // TODO: JSON'dan Deserialization (Etkinliğin ve biletin varlığını kontrol et, fiyatı al). 
            // MVP simülasyonu için 100₺ atanmıştır:
            return new ValidateTicketTypeResult(true, 100m, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Katalog ile iletişimde hata oluştu.");
            return new ValidateTicketTypeResult(false, 0, "Katalog entegrasyonu iletişim hatası.");
        }
    }
}
