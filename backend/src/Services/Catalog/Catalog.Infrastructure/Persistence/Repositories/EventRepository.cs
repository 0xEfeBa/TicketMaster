using Catalog.Application.Abstractions;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

public class EventRepository : IEventRepository
{
    private readonly CatalogDbContext _context;

    public EventRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Event?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Include(e => e.Sessions)
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<Event>> GetPublishedEventsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Where(e => e.Status == EventStatus.Published)
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public void Add(Event @event)
    {
        _context.Events.Add(@event);
    }

    public void Update(Event @event)
    {
        var entry = _context.Entry(@event);
        if (entry.State == EntityState.Detached)
        {
            _context.Events.Attach(@event);
            _context.Entry(@event).State = EntityState.Modified;
        }

        foreach (var ticketType in @event.TicketTypes)
        {
            var ttEntry = _context.Entry(ticketType);
            if (ttEntry.State == EntityState.Detached)
            {
                _context.TicketTypes.Attach(ticketType);
                ttEntry = _context.Entry(ticketType);
            }

            var existsInDatabase = _context.TicketTypes.Any(t => t.Id == ticketType.Id);
            if (!existsInDatabase && ttEntry.State != EntityState.Added)
                ttEntry.State = EntityState.Added;
        }
    }

    public void Delete(Event @event)
    {
        _context.Events.Remove(@event);
    }
}
