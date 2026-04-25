using Booking.Application.Abstractions;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Persistence.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly BookingDbContext _context;

    public ReservationRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .Include(r => r.Tickets)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<List<Reservation>> GetActiveReservationsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .Where(r => r.EventId == eventId && r.Status != ReservationStatus.Cancelled)
            .Include(r => r.Tickets)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Reservation>> GetUserReservationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Reservations
            .Where(r => r.UserId == userId)
            .Include(r => r.Tickets)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Add(Reservation reservation)
    {
        _context.Reservations.Add(reservation);
    }

    public void Update(Reservation reservation)
    {
        _context.Reservations.Update(reservation);
    }
}
