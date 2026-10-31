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

        var (hourlyRate, dailyCap, nightFlatRate) = GetVehicleRates(session.Vehicle);
        var normalFee = CalculateNormalFee(duration, hourlyRate, dailyCap);

        if (IsNightWindow(session.EntryTime, session.ExitTime))
            return Math.Min(normalFee, nightFlatRate);

        return normalFee;
    }

    private static (decimal HourlyRate, decimal DailyCap, decimal NightFlatRate) GetVehicleRates(VehicleType vehicle) =>
        vehicle switch
        {
            VehicleType.Motorcycle => (5m, 40m, 15m),
            VehicleType.Truck => (25m, 200m, 75m),
            _ => (10m, 80m, 30m)
        };

    private static decimal CalculateNormalFee(TimeSpan duration, decimal hourlyRate, decimal dailyCap)
    {
        var fullDays = (int)(duration.TotalHours / 24);
        var remainingMinutes = duration.TotalMinutes - (fullDays * 24 * 60);

        var remainingHours = (int)Math.Ceiling(remainingMinutes / 60.0);
        var remainingFee = Math.Min(remainingHours * hourlyRate, dailyCap);

        return (fullDays * dailyCap) + remainingFee;
    }

    private static bool IsNightWindow(DateTime entry, DateTime exit)
    {
        if (entry.TimeOfDay < TimeSpan.FromHours(22))
            return false;

        return exit.Date == entry.Date ||
               (exit.Date == entry.Date.AddDays(1) && exit.TimeOfDay <= TimeSpan.FromHours(8));
    }
}
