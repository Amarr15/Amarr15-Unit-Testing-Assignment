using DriveNow.Domain.Entities;
using DriveNow.Domain.ValueObjects;

namespace DriveNow.Application.Receipts;

public class LineItemFactory : ILineItemFactory
{
    public List<LineItem> Create(Booking booking, ReturnReport? report)
    {
        var items = new List<LineItem>();
        Add(items, "Base rental", booking.Price.BaseAmount);
        Add(items, "Weekend surcharge", booking.Price.WeekendSurcharge);
        Add(items, "Young driver surcharge", booking.Price.YoungDriverSurcharge);
        Add(items, "Duration discount", -booking.Price.DurationDiscount);
        Add(items, "Loyalty discount", -booking.Price.LoyaltyDiscount);
        Add(items, "Promo discount", -booking.Price.PromoDiscount);
        Add(items, "Insurance", booking.Price.InsuranceCost);
        Add(items, "Additional drivers", booking.Price.AdditionalDriversCost);
        if (report is not null)
        {
            Add(items, "Late fee", report.LateFee);
            Add(items, "Extra mileage", report.MileageFee);
            Add(items, "Fuel", report.FuelFee);
            Add(items, "Damage", report.DamageCharge);
        }

        return items;
    }

    static void Add(List<LineItem> items, string description, decimal amount)
    {
        if (amount != 0m)
            items.Add(new LineItem { Description = description, Amount = amount });
    }
}
