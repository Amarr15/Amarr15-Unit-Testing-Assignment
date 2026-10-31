namespace DriveNow.Domain.Exceptions;

public class BookingRejectedException : Exception
{
    public BookingRejectedException(string message) : base(message)
    {
    }
}
