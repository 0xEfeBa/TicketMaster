using Booking.Application.Abstractions;
using Booking.Domain.Exceptions;
using MediatR;

namespace Booking.Application.Features.Reservations.Commands.ConfirmReservation;

public class ConfirmReservationCommandHandler : IRequestHandler<ConfirmReservationCommand>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmReservationCommandHandler(IReservationRepository reservationRepository, IUnitOfWork unitOfWork)
    {
        _reservationRepository = reservationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ConfirmReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation is null)
            throw new BookingDomainException("Rezervasyon bulunamadı.");

        // Yetki kontrolü (Sadece rezerve eden edebilir)
        if (reservation.UserId != request.UserId)
            throw new BookingDomainException("Sadece rezervasyon sahibi işlemi yapabilir.");

        var now = DateTimeOffset.UtcNow;
        if (reservation.IsExpired(now))
            throw new BookingDomainException("Bu işlem için tanınan süre dolmuştur (Hold Süresi Bitti).");

        reservation.Confirm(now);
        _reservationRepository.Update(reservation);

        // Optimistic locking (RowVersion/xmin) bu save noktasında otomatik devreye girer.
        // Eğer aynı reservation'u/kaydı birden fazla kişi confirm(güncelleme) atmaya çalışırsa RowVersion koruyacaktır.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
