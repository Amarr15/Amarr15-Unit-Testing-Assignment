namespace DriveNow.Application.Receipts;

public class TaxCalculator
{
    public decimal CalculateVat(decimal amount) =>
        Math.Round(amount * 0.14m, 2, MidpointRounding.AwayFromZero);
}
