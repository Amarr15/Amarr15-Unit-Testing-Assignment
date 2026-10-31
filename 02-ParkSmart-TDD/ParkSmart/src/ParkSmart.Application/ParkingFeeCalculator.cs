using ParkSmart.Domain;

namespace ParkSmart.Application;

public class ParkingFeeCalculator
{
    public decimal Calculate(ParkingSession session)
    {
        var duration = session.ExitTime - session.EntryTime;
        if (duration.TotalMinutes <= 15)
            return 0m;

        return 10m;
    }
}
