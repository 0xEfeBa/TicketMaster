using Catalog.Application.Abstractions;
using TicketFlow.BuildingBlocks.Messaging;
using TicketFlow.BuildingBlocks.Messaging.Events;
using TicketFlow.BuildingBlocks.Messaging.Outbox;
using Catalog.Domain.Exceptions;
using MediatR;

namespace Catalog.Application.Features.Events.Commands.CancelEvent;

public class CancelEventCommandHandler : IRequestHandler<CancelEventCommand, Unit>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventOutbox _eventOutbox;

    public CancelEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork, IEventOutbox eventOutbox)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _eventOutbox = eventOutbox;
    }

    public async Task<Unit> Handle(CancelEventCommand request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null)
            throw new CatalogDomainException("Event not found.");

        if (!request.RequestingUserIsAdmin && @event.OrganizerUserId != request.RequestingUserId)
            throw new CatalogDomainException("Only the event owner or an Admin can perform this action.", isAccessDenied: true);

        @event.Cancel();
        _eventRepository.Update(@event);

        await _eventOutbox.SaveAsync(new EventCancelledIntegrationEvent(@event.Id), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
