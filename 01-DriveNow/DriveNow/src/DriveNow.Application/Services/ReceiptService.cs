using DriveNow.Application.Receipts;
using DriveNow.Domain.Entities;
using DriveNow.Domain.ValueObjects;

namespace DriveNow.Application.Services;

public class ReceiptService
{
    readonly LineItemFactory _lineItems;
    readonly TaxCalculator _tax;
    readonly CurrencyFormatter _currency;
    readonly ReceiptFormatter _receipts;

    public ReceiptService(
        LineItemFactory lineItems,
        TaxCalculator tax,
        CurrencyFormatter currency,
        ReceiptFormatter receipts)
    {
        _lineItems = lineItems;
        _tax = tax;
        _currency = currency;
        _receipts = receipts;
    }

    public ReceiptService()
        : this(new LineItemFactory(), new TaxCalculator(), new CurrencyFormatter(), new ReceiptFormatter())
    {
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
        var formattedItems = new List<(string Description, string FormattedAmount)>();
        var width = 0;
        foreach (var item in items)
        {
            var formatted = _currency.Format(item.Amount);
            formattedItems.Add((item.Description, formatted));
            width = Math.Max(width, formatted.Length);
        }

        var lines = new List<string>();
        foreach (var (description, formattedAmount) in formattedItems)
        {
            lines.Add(description.PadRight(30, '.') + formattedAmount.PadLeft(width));
        }

        lines.Add("VAT 14%".PadRight(30, '.') + _currency.Format(vat).PadLeft(width));
        lines.Add("Total".PadRight(30, '.') + _currency.Format(total).PadLeft(width));
        return _receipts.Render(booking.Reference, lines);
    }
}
