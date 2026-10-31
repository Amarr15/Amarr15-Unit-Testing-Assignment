using ParkSmart.Domain;

namespace ParkSmart.Application;

public class ParkingFeeCalculator
{
    public decimal Calculate(ParkingSession session)
    {
        if (session.ExitTime < session.EntryTime)
            throw new ArgumentException("Exit time must be after entry time");

        var duration = session.ExitTime - session.EntryTime;
        var isFriday = session.EntryTime.DayOfWeek == DayOfWeek.Friday;
        var freeMinutes = isFriday ? 120 : 15;

        if (!session.TicketLost && duration.TotalMinutes <= freeMinutes)
            return 0m;

        var (hourlyRate, dailyCap, nightFlatRate, lostTicketMin) = GetVehicleRates(session.Vehicle);
        var normalFee = CalculateNormalFee(duration, hourlyRate, dailyCap);

        var fee = IsNightWindow(session.EntryTime, session.ExitTime)
            ? Math.Min(normalFee, nightFlatRate)
            : normalFee;

        if (session.TicketLost)
            fee = Math.Max(fee, lostTicketMin);

        return fee;
    }

    private static (decimal HourlyRate, decimal DailyCap, decimal NightFlatRate, decimal LostTicketMin) GetVehicleRates(VehicleType vehicle) =>
        vehicle switch
        {
            VehicleType.Motorcycle => (5m, 40m, 15m, 75m),
            VehicleType.Truck => (25m, 200m, 75m, 300m),
            _ => (10m, 80m, 30m, 150m)
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
