namespace Booking.Domain.Entities;

public class Ticket
{
    public Guid Id { get; private set; }
    public Guid ReservationId { get; private set; }
    public Guid TicketTypeId { get; private set; } // Referans to Catalog
    public decimal PriceAmount { get; private set; }
    public uint Version { get; private set; }

    private Ticket() { }

    internal Ticket(Guid id, Guid reservationId, Guid ticketTypeId, decimal priceAmount)
    {
        Id = id;
        ReservationId = reservationId;
        TicketTypeId = ticketTypeId;
        PriceAmount = priceAmount;
    }
}
