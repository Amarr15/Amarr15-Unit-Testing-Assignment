namespace DriveNow.Infrastructure.External;

public class PaymentFailedException : Exception
{
    public PaymentFailedException(string message) : base(message)
    {
    }
}
