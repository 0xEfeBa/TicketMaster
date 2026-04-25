using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace TicketFlow.UnitTests.Domain;

public class ReservationTests
{
    [Fact]
    public void Constructor_Should_InitializeWithHeldStatus()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var holdDuration = TimeSpan.FromMinutes(10);

        // Act
        var reservation = new Reservation(id, userId, eventId, holdDuration);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Held);
        reservation.ExpiresAtUtc.Should().BeAfter(reservation.CreatedAt);
    }

    [Fact]
    public void Confirm_Should_ChangeStatusToConfirmed_When_NotExpired()
    {
        // Arrange
        var reservation = new Reservation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromMinutes(10));
        var now = DateTimeOffset.UtcNow;

        // Act
        reservation.Confirm(now);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
    }

    [Fact]
    public void Confirm_Should_ThrowException_When_Expired()
    {
        // Arrange
        var reservation = new Reservation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromMinutes(-1));
        var now = DateTimeOffset.UtcNow;

        // Act
        var act = () => reservation.Confirm(now);

        // Assert
        act.Should().Throw<BookingDomainException>().WithMessage("*dolmuş*");
    }

    [Fact]
    public void InvalidateDueToEventCancellation_Should_SetStatusToCancelled()
    {
        // Arrange
        var reservation = new Reservation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromMinutes(10));

        // Act
        reservation.InvalidateDueToEventCancellation();

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }
}
