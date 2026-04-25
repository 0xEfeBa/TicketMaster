using Catalog.Application.Abstractions.Caching;

namespace Catalog.Application.Features.Events.Queries.GetEvents;

public record EventDto(Guid Id, string Title, string Venue, string? ImageUrl, DateTimeOffset CreatedAt);

public record GetEventsQuery(int Page, int PageSize) : ICacheableRequest<List<EventDto>>
{
    // Caching pipeline'ı bu metodu kullanarak Redis Key'ini belirler.
    public string CacheKey => $"events:list:published:v1:page{Page}:size{PageSize}";
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
}
