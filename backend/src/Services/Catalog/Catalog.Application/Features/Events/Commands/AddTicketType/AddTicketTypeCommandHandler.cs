using Catalog.Application.Abstractions;
using Catalog.Domain.Exceptions;
using MediatR;

namespace Catalog.Application.Features.Events.Commands.AddTicketType;

public class AddTicketTypeCommandHandler : IRequestHandler<AddTicketTypeCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddTicketTypeCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AddTicketTypeCommand request, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null)
            throw new CatalogDomainException("Etkinlik bulunamadı.");

        if (!request.RequestingUserIsAdmin && @event.OrganizerUserId != request.RequestingUserId)
            throw new CatalogDomainException("Sadece etkinliğin sahibi veya Admin bilet ekleyebilir.", isAccessDenied: true);

        var newTicketTypeId = Guid.NewGuid();
        @event.AddTicketType(newTicketTypeId, request.Name, request.PriceAmount, request.TotalQuantity);

        _eventRepository.Update(@event);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return newTicketTypeId;
    }
}
