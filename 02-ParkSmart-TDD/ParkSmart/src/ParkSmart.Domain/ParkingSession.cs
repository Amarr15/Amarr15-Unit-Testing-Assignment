namespace ParkSmart.Domain;

public class ParkingSession
{
    public DateTime EntryTime { get; set; }
    public DateTime ExitTime { get; set; }
    public VehicleType Vehicle { get; set; } = VehicleType.Car;
    public bool IsSubscriber { get; set; }
    public bool TicketLost { get; set; }
    public decimal ChargedKwh { get; set; }
}
