using Catalog.Application.Abstractions;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.Features.Events.Commands.CreateEvent;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var @event = new Event(
            Guid.NewGuid(),
            request.Title,
            request.Description,
            request.Venue,
            request.ImageUrl,
            request.OrganizerUserId);

        _eventRepository.Add(@event);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return @event.Id;
    }
}
