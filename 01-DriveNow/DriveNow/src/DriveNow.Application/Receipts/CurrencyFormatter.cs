using System.Globalization;

namespace DriveNow.Application.Receipts;

public class CurrencyFormatter
{
    public string Format(decimal amount) =>
        amount.ToString("#,##0.00", CultureInfo.InvariantCulture) + " EGP";
}
