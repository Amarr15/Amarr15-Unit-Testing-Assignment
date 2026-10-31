using DriveNow.Domain.Enums;

namespace DriveNow.Domain.Entities;

public class PromoCode
{
    public string Code { get; set; } = string.Empty;
    public int DiscountPercent { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public int MinDays { get; set; }
    public List<VehicleCategory>? AllowedCategories { get; set; }
    public int MaxUses { get; set; }
    public int UsedCount { get; set; }
}
