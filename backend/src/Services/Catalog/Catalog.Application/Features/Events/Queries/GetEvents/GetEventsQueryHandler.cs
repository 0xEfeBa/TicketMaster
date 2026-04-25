using Catalog.Application.Abstractions;
using MediatR;

namespace Catalog.Application.Features.Events.Queries.GetEvents;

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, List<EventDto>>
{
    private readonly IEventRepository _eventRepository;

    public GetEventsQueryHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    // Handler içerisine Redis kodları SIZDIRILMAZ.
    public async Task<List<EventDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var rawEvents = await _eventRepository.GetPublishedEventsAsync(request.Page, request.PageSize, cancellationToken);
        
        return rawEvents.Select(e => new EventDto(
            e.Id,
            e.Title,
            e.Venue,
            e.ImageUrl,
            e.CreatedAt
        )).ToList();
    }
}
