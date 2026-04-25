using Catalog.Domain.Enums;
using MediatR;

namespace Catalog.Application.Features.Events.Queries.GetEventById;

public record SessionDto(Guid Id, DateTimeOffset StartsAt, DateTimeOffset? EndsAt);
public record TicketTypeDto(Guid Id, string Name, decimal PriceAmount, int TotalQuantity);

public record EventDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Venue,
    string? ImageUrl,
    EventStatus Status,
    Guid OrganizerUserId,
    List<SessionDto> Sessions,
    List<TicketTypeDto> TicketTypes);

public record GetEventByIdQuery(Guid EventId, Guid? ViewerUserId, string? ViewerRoleName) : IRequest<EventDetailDto?>;
