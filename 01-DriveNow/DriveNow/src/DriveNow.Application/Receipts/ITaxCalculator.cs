namespace DriveNow.Application.Receipts;

public interface ITaxCalculator
{
    decimal CalculateVat(decimal amount);
}
