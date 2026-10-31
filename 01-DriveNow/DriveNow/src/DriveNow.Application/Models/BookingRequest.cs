using DriveNow.Domain.Enums;

namespace DriveNow.Application.Models;

public class BookingRequest
{
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public DateTime PickupAt { get; set; }
    public DateTime ReturnAt { get; set; }
    public InsurancePlan InsurancePlan { get; set; }
    public int AdditionalDrivers { get; set; }
    public string? PromoCode { get; set; }
}
