using DriveNow.Domain.Enums;
using DriveNow.Domain.ValueObjects;

namespace DriveNow.Domain.Entities;

public class Booking
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public DateTime PickupAt { get; set; }
    public DateTime ReturnAt { get; set; }
    public DateTime? ActualReturnAt { get; set; }
    public InsurancePlan InsurancePlan { get; set; }
    public int AdditionalDrivers { get; set; }
    public string? PromoCode { get; set; }
    public PriceBreakdown Price { get; set; } = new();
    public decimal Deposit { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int StartMileage { get; set; }
    public int? EndMileage { get; set; }
    public decimal? ExtraCharges { get; set; }
}
