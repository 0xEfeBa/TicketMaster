using Booking.Application.Abstractions;
using Booking.Domain.Entities;
using Booking.Domain.Exceptions;
using MediatR;

namespace Booking.Application.Features.Reservations.Commands.CreateHold;

public class CreateHoldCommandHandler : IRequestHandler<CreateHoldCommand, Guid>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ICatalogClient _catalogClient;
    private readonly IUnitOfWork _unitOfWork;

    public CreateHoldCommandHandler(IReservationRepository reservationRepository, ICatalogClient catalogClient, IUnitOfWork unitOfWork)
    {
        _reservationRepository = reservationRepository;
        _catalogClient = catalogClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateHoldCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _catalogClient.ValidateTicketTypeAsync(request.EventId, request.TicketTypeId, request.ReqBearerToken, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            throw new BookingDomainException($"Catalog validation failed: {validationResult.ErrorMessage}");
        }

        var reservation = new Reservation(Guid.NewGuid(), request.UserId, request.EventId, TimeSpan.FromMinutes(10));

        for(int i = 0; i < request.Quantity; i++)
        {
            reservation.AddTicket(Guid.NewGuid(), request.TicketTypeId, validationResult.Price);
        }

        _reservationRepository.Add(reservation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return reservation.Id;
    }
}
