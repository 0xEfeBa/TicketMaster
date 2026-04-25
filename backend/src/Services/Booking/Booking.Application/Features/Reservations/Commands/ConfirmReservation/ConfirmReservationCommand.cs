using MediatR;

namespace Booking.Application.Features.Reservations.Commands.ConfirmReservation;

public record ConfirmReservationCommand(Guid ReservationId, Guid UserId) : IRequest;
