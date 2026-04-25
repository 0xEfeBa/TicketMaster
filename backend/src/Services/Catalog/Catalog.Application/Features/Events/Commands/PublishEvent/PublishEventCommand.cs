using Catalog.Application.Abstractions.Caching;
using MediatR;

namespace Catalog.Application.Features.Events.Commands.PublishEvent;

public record PublishEventCommand(Guid EventId, Guid RequestingUserId, bool RequestingUserIsAdmin) : ICacheInvalidatorRequest<Unit>
{
    public IReadOnlyList<string> CacheKeysToRemove => EventCacheKeys.ForPublishOrCancel(EventId);
}
