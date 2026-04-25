using Catalog.Domain.Entities;

namespace Catalog.Application.Abstractions;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Event>> GetPublishedEventsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    void Add(Event @event);
    void Update(Event @event);
    void Delete(Event @event);
}
