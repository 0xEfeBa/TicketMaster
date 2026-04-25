using Booking.Domain.Enums;
using Booking.Domain.Exceptions;

namespace Booking.Domain.Entities;

public class Reservation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; } // Identity'den (sub) olan kopya referans
    public Guid EventId { get; private set; } // Catalog'dan referans
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public uint Version { get; private set; } // PG xmin optimizasyonu

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
            throw new BookingDomainException("Bilet sadece Held statüsündeki bir rezervasyona eklenebilir.");
        }

        var ticket = new Ticket(ticketId, Id, ticketTypeId, priceAmount);
        _tickets.Add(ticket);
    }

    /// <summary>
    /// Satın alma basamaklarında Hold süresi geçmiş mi diye On-The-Fly doğrulama yapar.
    /// </summary>
    public bool IsExpired(DateTimeOffset now)
    {
        return Status == ReservationStatus.Held && ExpiresAtUtc < now;
    }

    public void Confirm(DateTimeOffset now)
    {
        if (Status == ReservationStatus.Confirmed)
        {
            throw new BookingDomainException("Rezervasyon zaten onaylanmış.");
        }

        if (Status == ReservationStatus.Cancelled)
        {
            throw new BookingDomainException("İptal edilmiş bir rezervasyon onaylanamaz.");
        }

        if (IsExpired(now))
        {
            throw new BookingDomainException("Bu rezervasyonun hold süresi (geçerlilik tarihi) dolmuş.");
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
        // Planda belirtildiği üzere: Etkinlik iptal edildiğinde rezervasyonu geçersiz kıl
        Status = ReservationStatus.Cancelled;
        // İleride özel bir status (InvalidatedDueToEventCancellation) eklenecekse burası güncellenir.
    }
}
