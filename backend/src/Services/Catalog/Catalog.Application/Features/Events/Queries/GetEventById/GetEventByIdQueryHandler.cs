using Catalog.Application.Abstractions;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using MediatR;

namespace Catalog.Application.Features.Events.Queries.GetEventById;

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventDetailDto?>
{
    private readonly IEventRepository _eventRepository;

    public GetEventByIdQueryHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<EventDetailDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdWithDetailsAsync(request.EventId, cancellationToken);
        if (@event is null) return null;

        if (!CanView(@event, request.ViewerUserId, request.ViewerRoleName))
            return null;

        return new EventDetailDto(
            @event.Id,
            @event.Title,
            @event.Description,
            @event.Venue,
            @event.ImageUrl,
            @event.Status,
            @event.OrganizerUserId,
            @event.Sessions.Select(s => new SessionDto(s.Id, s.StartsAt, s.EndsAt)).ToList(),
            @event.TicketTypes.Select(t => new TicketTypeDto(t.Id, t.Name, t.PriceAmount, t.TotalQuantity)).ToList()
        );
    }

    private static bool CanView(Event @event, Guid? userId, string? role)
    {
        if (@event.Status == EventStatus.Published)
            return true;

        if (userId is null || string.IsNullOrEmpty(role))
            return false;

        if (role == "Admin")
            return true;

        if (role == "Organizer" && @event.OrganizerUserId == userId.Value)
            return true;

        return false;
    }
}
