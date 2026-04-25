using System.Text.Json;
using Catalog.Application.Abstractions.Caching;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.Application.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;

    public CachingBehavior(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is ICacheInvalidatorRequest<TResponse> invalidator)
        {
            foreach (var key in invalidator.CacheKeysToRemove)
                await _cache.RemoveAsync(key, cancellationToken);
        }

        if (request is not ICacheableRequest<TResponse> cacheableRequest)
        {
            return await next();
        }

        var cachedResponseBytes = await _cache.GetAsync(cacheableRequest.CacheKey, cancellationToken);
        if (cachedResponseBytes != null)
        {
            var cachedResponse = JsonSerializer.Deserialize<TResponse>(cachedResponseBytes);
            if (cachedResponse != null)
            {
                return cachedResponse;
            }
        }

        var response = await next();

        var options = new DistributedCacheEntryOptions();
        if (cacheableRequest.SlidingExpiration.HasValue)
        {
            options.SetSlidingExpiration(cacheableRequest.SlidingExpiration.Value);
        }
        else
        {
            options.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
        }

        var responseBytes = JsonSerializer.SerializeToUtf8Bytes(response);
        await _cache.SetAsync(cacheableRequest.CacheKey, responseBytes, options, cancellationToken);

        return response;
    }
}
