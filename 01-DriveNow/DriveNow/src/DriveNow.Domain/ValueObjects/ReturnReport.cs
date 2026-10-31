namespace DriveNow.Domain.ValueObjects;

public class ReturnReport
{
    public int LateMinutes { get; set; }
    public decimal LateFee { get; set; }
    public int AllowedKm { get; set; }
    public int ExtraKm { get; set; }
    public decimal MileageFee { get; set; }
    public int MissingFuelQuarters { get; set; }
    public decimal FuelFee { get; set; }
    public decimal DamageCharge { get; set; }
    public decimal TotalExtra { get; set; }
}
