using DriveNow.Domain.Entities;
using DriveNow.Domain.ValueObjects;

namespace DriveNow.Application.Receipts;

public class ReceiptService
{
    readonly ILineItemFactory _lineItems;
    readonly ITaxCalculator _tax;
    readonly ICurrencyFormatter _currency;
    readonly IReceiptFormatter _receipts;

    public ReceiptService(
        ILineItemFactory lineItems,
        ITaxCalculator tax,
        ICurrencyFormatter currency,
        IReceiptFormatter receipts)
    {
        _lineItems = lineItems;
        _tax = tax;
        _currency = currency;
        _receipts = receipts;
    }

    public string Build(Booking booking, ReturnReport? report)
    {
        var items = _lineItems.Create(booking, report);
        decimal vat = 0m;
        decimal net = 0m;
        foreach (var item in items)
        {
            vat += _tax.CalculateVat(item.Amount);
            net += item.Amount;
        }

        var total = net + vat;
        var width = 0;
        foreach (var item in items)
            width = Math.Max(width, _currency.Format(item.Amount).Length);

        var lines = new List<string>();
        foreach (var item in items)
        {
            var amount = _currency.Format(item.Amount);
            lines.Add(item.Description.PadRight(30, '.') + amount.PadLeft(width));
        }

        lines.Add("VAT 14%".PadRight(30, '.') + _currency.Format(vat).PadLeft(width));
        lines.Add("Total".PadRight(30, '.') + _currency.Format(total).PadLeft(width));
        return _receipts.Render(booking.Reference, lines);
    }
}
