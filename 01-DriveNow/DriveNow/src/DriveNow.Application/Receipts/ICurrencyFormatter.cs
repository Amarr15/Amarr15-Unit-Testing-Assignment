namespace DriveNow.Application.Receipts;

public interface ICurrencyFormatter
{
    string Format(decimal amount);
}
