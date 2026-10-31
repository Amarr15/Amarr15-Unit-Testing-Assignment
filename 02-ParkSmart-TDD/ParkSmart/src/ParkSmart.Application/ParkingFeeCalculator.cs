using ParkSmart.Domain;

namespace ParkSmart.Application;

public class ParkingFeeCalculator
{
    public decimal Calculate(ParkingSession session)
    {
        if (session.ExitTime < session.EntryTime)
            throw new ArgumentException("Exit time must be after entry time");

        var duration = session.ExitTime - session.EntryTime;
        if (duration.TotalMinutes <= 15)
            return 0m;

        var (hourlyRate, dailyCap, nightFlatRate) = session.Vehicle switch
        {
            VehicleType.Motorcycle => (5m, 40m, 15m),
            VehicleType.Truck => (25m, 200m, 75m),
            _ => (10m, 80m, 30m)
        };

        var fullDays = (int)(duration.TotalHours / 24);
        var remainingMinutes = duration.TotalMinutes - (fullDays * 24 * 60);

        var remainingHours = (int)Math.Ceiling(remainingMinutes / 60.0);
        var remainingFee = Math.Min(remainingHours * hourlyRate, dailyCap);
        var normalFee = (fullDays * dailyCap) + remainingFee;

        var isNightWindow = session.EntryTime.TimeOfDay >= TimeSpan.FromHours(22) &&
            ((session.ExitTime.Date == session.EntryTime.Date) ||
             (session.ExitTime.Date == session.EntryTime.Date.AddDays(1) && session.ExitTime.TimeOfDay <= TimeSpan.FromHours(8)));

        if (isNightWindow)
            return Math.Min(normalFee, nightFlatRate);

        return normalFee;
    }
}
