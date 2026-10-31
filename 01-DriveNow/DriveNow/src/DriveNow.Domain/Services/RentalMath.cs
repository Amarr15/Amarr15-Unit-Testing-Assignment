namespace DriveNow.Domain.Services;

public static class RentalMath
{
    public static int CalculateAge(DateTime dateOfBirth, DateTime onDate) =>
        FullYears(dateOfBirth, onDate);

    public static int CalculateLicenseYears(DateTime issueDate, DateTime onDate) =>
        FullYears(issueDate, onDate);

    public static int CalculateRentalDays(DateTime pickup, DateTime returnAt)
    {
        if (returnAt <= pickup)
            throw new ArgumentException("Return must be after pickup");

        var span = returnAt - pickup;
        var days = (int)(span.Ticks / TimeSpan.TicksPerDay);
        var remainder = TimeSpan.FromTicks(span.Ticks % TimeSpan.TicksPerDay);
        if (remainder.TotalMinutes >= 60)
            days++;

        return Math.Max(days, 1);
    }

    static int FullYears(DateTime start, DateTime onDate)
    {
        var years = onDate.Year - start.Year;
        if (onDate.Date < Birthday(start, onDate.Year))
            years--;

        return years;
    }

    static DateTime Birthday(DateTime start, int year)
    {
        var day = start.Day;
        if (start.Month == 2 && day == 29 && !DateTime.IsLeapYear(year))
            day = 28;

        return new DateTime(year, start.Month, day);
    }
}
