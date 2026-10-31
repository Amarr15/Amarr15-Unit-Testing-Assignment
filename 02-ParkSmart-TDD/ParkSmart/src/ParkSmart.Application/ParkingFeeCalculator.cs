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

        var fullDays = (int)(duration.TotalHours / 24);
        var remainingMinutes = duration.TotalMinutes - (fullDays * 24 * 60);

        var remainingHours = (int)Math.Ceiling(remainingMinutes / 60.0);
        var remainingFee = Math.Min(remainingHours * 10m, 80m);

        return (fullDays * 80m) + remainingFee;
    }
}
