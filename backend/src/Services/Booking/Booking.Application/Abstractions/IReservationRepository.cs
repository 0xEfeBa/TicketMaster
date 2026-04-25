using Booking.Domain.Entities;

namespace Booking.Application.Abstractions;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Reservation>> GetActiveReservationsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<List<Reservation>> GetUserReservationsAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(Reservation reservation);
    void Update(Reservation reservation);
}
