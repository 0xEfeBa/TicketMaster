using Booking.Domain.Enums;
using Booking.Domain.Exceptions;

namespace Booking.Domain.Entities;

public class Reservation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid EventId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public uint Version { get; private set; }

    private readonly List<Ticket> _tickets = new();
    public IReadOnlyCollection<Ticket> Tickets => _tickets.AsReadOnly();

    private Reservation() { }

    public Reservation(Guid id, Guid userId, Guid eventId, TimeSpan holdDuration)
    {
        Id = id;
        UserId = userId;
        EventId = eventId;
        Status = ReservationStatus.Held;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAtUtc = CreatedAt.Add(holdDuration);
    }

    public void AddTicket(Guid ticketId, Guid ticketTypeId, decimal priceAmount)
    {
        if (Status != ReservationStatus.Held)
        {
            throw new BookingDomainException("Tickets can only be added while the reservation is in Held status.");
        }

        var ticket = new Ticket(ticketId, Id, ticketTypeId, priceAmount);
        _tickets.Add(ticket);
    }

    public bool IsExpired(DateTimeOffset now)
    {
        return Status == ReservationStatus.Held && ExpiresAtUtc < now;
    }

    public void Confirm(DateTimeOffset now)
    {
        if (Status == ReservationStatus.Confirmed)
        {
            throw new BookingDomainException("Reservation is already confirmed.");
        }

        if (Status == ReservationStatus.Cancelled)
        {
            throw new BookingDomainException("A cancelled reservation cannot be confirmed.");
        }

        if (IsExpired(now))
        {
            throw new BookingDomainException("The hold period for this reservation has expired.");
        }

        Status = ReservationStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status == ReservationStatus.Cancelled)
            return;

        Status = ReservationStatus.Cancelled;
    }

    public void InvalidateDueToEventCancellation()
    {
        Status = ReservationStatus.Cancelled;
    }
}
