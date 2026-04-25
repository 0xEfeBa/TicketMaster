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
            throw new CatalogDomainException("Etkinlik bulunamadı.");

        if (!request.RequestingUserIsAdmin && @event.OrganizerUserId != request.RequestingUserId)
            throw new CatalogDomainException("Sadece etkinliğin yetkili sahibi veya Admin işlemi yapabilir.", isAccessDenied: true);

        @event.Publish(); // Will throw Domain Exception if no tickets or sessions exist!
        
        _eventRepository.Update(@event);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
