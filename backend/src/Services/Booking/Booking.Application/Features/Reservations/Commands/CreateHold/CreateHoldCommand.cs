using MediatR;

namespace Booking.Application.Features.Reservations.Commands.CreateHold;

public record CreateHoldCommand(
    Guid UserId, 
    Guid EventId, 
    Guid TicketTypeId, 
    int Quantity,
    string ReqBearerToken) : IRequest<Guid>;
