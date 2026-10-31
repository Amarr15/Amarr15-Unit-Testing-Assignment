namespace DriveNow.Domain.Exceptions;

public class InvalidPromoCodeException : Exception
{
    public InvalidPromoCodeException(string message) : base(message)
    {
    }
}
