using DriveNow.Domain.Enums;

namespace DriveNow.Domain.Entities;

public class Vehicle
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public VehicleCategory Category { get; set; }
    public decimal DailyRate { get; set; }
    public int Mileage { get; set; }
    public bool IsAvailable { get; set; }
}
