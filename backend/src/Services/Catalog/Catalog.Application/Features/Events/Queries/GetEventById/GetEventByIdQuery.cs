using Catalog.Domain.Enums;
using MediatR;

namespace Catalog.Application.Features.Events.Queries.GetEventById;

// These DTOs represent the aggregate projection
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

/// <summary>
/// Detay görünürlüğü: yalnızca Published herkese; Draft/İptal için Admin veya ilgili Organizer.
/// (Önbellek kullanılmıyor — rol bazlı farklı gövde riski.)
/// </summary>
public record GetEventByIdQuery(Guid EventId, Guid? ViewerUserId, string? ViewerRoleName) : IRequest<EventDetailDto?>;
