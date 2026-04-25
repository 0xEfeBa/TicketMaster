using Catalog.Domain.Enums;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Venue { get; private set; } = default!;
    public string? ImageUrl { get; private set; }
    public EventStatus Status { get; private set; }
    public Guid OrganizerUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public uint Version { get; private set; } // Used for xmin optimistic concurrency in PostgreSQL

    private readonly List<Session> _sessions = new();
    public IReadOnlyCollection<Session> Sessions => _sessions.AsReadOnly();

    private readonly List<TicketType> _ticketTypes = new();
    public IReadOnlyCollection<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    private Event() { } // For EF Core

    public Event(Guid id, string title, string description, string venue, string? imageUrl, Guid organizerUserId)
    {
        Id = id;
        Title = title;
        Description = description;
        Venue = venue;
        ImageUrl = imageUrl;
        Status = EventStatus.Draft;
        OrganizerUserId = organizerUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDetails(string title, string description, string venue, string? imageUrl)
    {
        Title = title;
        Description = description;
        Venue = venue;
        ImageUrl = imageUrl;
    }

    public void Publish()
    {
        if (Status == EventStatus.Published)
            throw new CatalogDomainException("Etkinlik zaten yayında.");

        if (!_sessions.Any())
            throw new CatalogDomainException("Yayınlamak için en az bir seans olmalıdır.");

        if (!_ticketTypes.Any())
            throw new CatalogDomainException("Yayınlamak için en az bir bilet türü olmalıdır.");

        Status = EventStatus.Published;
    }

    public void Cancel()
    {
        if (Status == EventStatus.Cancelled)
            return;

        Status = EventStatus.Cancelled;
    }

    public void AddSession(Guid id, DateTimeOffset startsAt, DateTimeOffset? endsAt)
    {
        if (endsAt.HasValue && endsAt.Value <= startsAt)
            throw new CatalogDomainException("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        var session = new Session(id, Id, startsAt, endsAt);
        _sessions.Add(session);
    }
    
    public void RemoveSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session is not null)
            _sessions.Remove(session);
    }

    public void AddTicketType(Guid id, string name, decimal priceAmount, int totalQuantity)
    {
        if (priceAmount < 0)
            throw new CatalogDomainException("Bilet fiyatı 0'dan küçük olamaz.");
            
        if (totalQuantity <= 0)
            throw new CatalogDomainException("Bilet miktarı 0'dan büyük olmalıdır.");

        var ticketType = new TicketType(id, Id, name, priceAmount, totalQuantity);
        _ticketTypes.Add(ticketType);
    }
    
    public void RemoveTicketType(Guid ticketTypeId)
    {
        var ticketType = _ticketTypes.FirstOrDefault(t => t.Id == ticketTypeId);
        if (ticketType is not null)
            _ticketTypes.Remove(ticketType);
    }
}
