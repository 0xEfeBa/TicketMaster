namespace Booking.Domain.Exceptions;

public class BookingConcurrencyException : Exception
{
    public BookingConcurrencyException(string message) : base(message)
    {
    }

    public BookingConcurrencyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
