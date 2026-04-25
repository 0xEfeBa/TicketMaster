using Booking.Application.Abstractions;
using TicketFlow.BuildingBlocks.Messaging;
using TicketFlow.BuildingBlocks.Messaging.Events;

namespace Booking.Application.Features.Reservations.IntegrationEvents;

public class EventCancelledIntegrationEventHandler(
    IReservationRepository reservationRepository,
    IUnitOfWork unitOfWork) : IIntegrationEventHandler<EventCancelledIntegrationEvent>
{
    public async Task HandleAsync(EventCancelledIntegrationEvent @event, CancellationToken ct = default)
    {
        var reservations = await reservationRepository.GetActiveReservationsByEventIdAsync(@event.EventId, ct);

        foreach (var reservation in reservations)
        {
            reservation.InvalidateDueToEventCancellation();
            reservationRepository.Update(reservation);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
