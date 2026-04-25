using MediatR;

namespace Catalog.Application.Abstractions.Caching;

public interface ICacheableRequest<TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }
    TimeSpan? SlidingExpiration { get; }
}

public interface ICacheInvalidatorRequest<TResponse> : IRequest<TResponse>
{
    IReadOnlyList<string> CacheKeysToRemove { get; }
}
