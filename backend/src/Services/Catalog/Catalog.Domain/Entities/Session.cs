namespace Catalog.Domain.Entities;

public class Session
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset? EndsAt { get; private set; }

    private Session() { }

    internal Session(Guid id, Guid eventId, DateTimeOffset startsAt, DateTimeOffset? endsAt)
    {
        Id = id;
        EventId = eventId;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public void Update(DateTimeOffset startsAt, DateTimeOffset? endsAt)
    {
        StartsAt = startsAt;
        EndsAt = endsAt;
    }
}
