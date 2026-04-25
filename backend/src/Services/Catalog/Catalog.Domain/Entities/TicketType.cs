namespace Catalog.Domain.Entities;

public class TicketType
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal PriceAmount { get; private set; }
    public int TotalQuantity { get; private set; }
    public uint Version { get; private set; } // Used for xmin optimistic concurrency in PostgreSQL

    private TicketType() { } // For EF Core

    internal TicketType(Guid id, Guid eventId, string name, decimal priceAmount, int totalQuantity)
    {
        Id = id;
        EventId = eventId;
        Name = name;
        PriceAmount = priceAmount;
        TotalQuantity = totalQuantity;
    }

    public void Update(string name, decimal priceAmount, int totalQuantity)
    {
        Name = name;
        PriceAmount = priceAmount;
        TotalQuantity = totalQuantity;
    }
}
