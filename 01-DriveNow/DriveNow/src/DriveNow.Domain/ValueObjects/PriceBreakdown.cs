namespace DriveNow.Domain.ValueObjects;

public class PriceBreakdown
{
    public int RentalDays { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal WeekendSurcharge { get; set; }
    public decimal YoungDriverSurcharge { get; set; }
    public decimal DurationDiscount { get; set; }
    public decimal LoyaltyDiscount { get; set; }
    public decimal PromoDiscount { get; set; }
    public decimal InsuranceCost { get; set; }
    public decimal AdditionalDriversCost { get; set; }
    public decimal Total { get; set; }
}
