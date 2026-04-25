using Catalog.Application.Abstractions;
using Catalog.Domain.Exceptions;
using MediatR;

namespace Catalog.Application.Features.Events.Commands.PublishEvent;

public class PublishEventCommandHandler : IRequestHandler<PublishEventCommand, Unit>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PublishEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdWithDetailsAsync(request.EventId, cancellationToken);
        if (@event is null)
            throw new CatalogDomainException("Event not found.");

        if (!request.RequestingUserIsAdmin && @event.OrganizerUserId != request.RequestingUserId)
            throw new CatalogDomainException("Only the event owner or an Admin can perform this action.", isAccessDenied: true);

        @event.Publish();

        _eventRepository.Update(@event);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
