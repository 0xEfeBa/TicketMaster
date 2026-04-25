namespace Booking.Application.Abstractions;

public record ValidateTicketTypeResult(bool IsValid, decimal Price, string ErrorMessage);

public interface ICatalogClient
{
    Task<ValidateTicketTypeResult> ValidateTicketTypeAsync(Guid eventId, Guid ticketTypeId, string bearerToken, CancellationToken cancellationToken = default);
}
