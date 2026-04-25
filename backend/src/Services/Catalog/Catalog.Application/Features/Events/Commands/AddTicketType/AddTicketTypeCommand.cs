using Catalog.Application.Abstractions.Caching;
using MediatR;

namespace Catalog.Application.Features.Events.Commands.AddTicketType;

public record AddTicketTypeCommand(Guid EventId, string Name, decimal PriceAmount, int TotalQuantity, Guid RequestingUserId, bool RequestingUserIsAdmin) : ICacheInvalidatorRequest<Guid>
{
    public IReadOnlyList<string> CacheKeysToRemove =>
    [
        $"event:detail:pub:{EventId}",
        $"event:detail:{EventId}"
    ];
}
