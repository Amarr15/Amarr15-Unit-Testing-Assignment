using FluentAssertions;
using ParkSmart.Application;
using ParkSmart.Domain;
using Xunit;

namespace ParkSmart.UnitTests;

public class ParkingFeeCalculatorTests
{
    private readonly ParkingFeeCalculator _sut = new();

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    public void Calculate_DurationUpToFifteenMinutes_ReturnsZeroFee(int minutes)
    {
        var entry = new DateTime(2026, 11, 1, 10, 0, 0);
        var session = new ParkingSession
        {
            EntryTime = entry,
            ExitTime = entry.AddMinutes(minutes),
            Vehicle = VehicleType.Car
        };

        var fee = _sut.Calculate(session);

        fee.Should().Be(0m);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(60)]
    public void Calculate_BetweenSixteenAndSixtyMinutes_ChargesTenEgp(int minutes)
    {
        var entry = new DateTime(2026, 11, 1, 10, 0, 0);
        var session = new ParkingSession
        {
            EntryTime = entry,
            ExitTime = entry.AddMinutes(minutes),
            Vehicle = VehicleType.Car
        };

        var fee = _sut.Calculate(session);

        fee.Should().Be(10m);
    }

    [Fact]
    public void Calculate_SixtyOneMinutes_ChargesTwoHours()
    {
        var entry = new DateTime(2026, 11, 1, 10, 0, 0);
        var session = new ParkingSession
        {
            EntryTime = entry,
            ExitTime = entry.AddMinutes(61),
            Vehicle = VehicleType.Car
        };

        var fee = _sut.Calculate(session);

        fee.Should().Be(20m);
    }

    [Fact]
    public void Calculate_ExitBeforeEntry_ThrowsArgumentException()
    {
        var entry = new DateTime(2026, 11, 1, 10, 0, 0);
        var session = new ParkingSession
        {
            EntryTime = entry,
            ExitTime = entry.AddMinutes(-1),
            Vehicle = VehicleType.Car
        };

        var act = () => _sut.Calculate(session);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Exit time must be after entry time");
    }

    [Theory]
    [InlineData(10, 0, 80)]
    [InlineData(30, 0, 140)]
    [InlineData(48, 0, 160)]
    [InlineData(48, 10, 170)]
    public void Calculate_LongDuration_AppliesDailyCapOfEightyPerTwentyFourHours(int hours, int minutes, decimal expectedFee)
    {
        var entry = new DateTime(2026, 11, 1, 10, 0, 0);
        var session = new ParkingSession
        {
            EntryTime = entry,
            ExitTime = entry.AddHours(hours).AddMinutes(minutes),
            Vehicle = VehicleType.Car
        };

        var fee = _sut.Calculate(session);

        fee.Should().Be(expectedFee);
    }

    [Theory]
    [InlineData(VehicleType.Motorcycle, 3, 0, 15)]
    [InlineData(VehicleType.Truck, 10, 0, 200)]
    [InlineData(VehicleType.Truck, 30, 0, 350)]
    public void Calculate_VehicleType_AppliesTypeSpecificHourlyRateAndDailyCap(
        VehicleType vehicle, int hours, int minutes, decimal expectedFee)
    {
        var entry = new DateTime(2026, 11, 1, 10, 0, 0);
        var session = new ParkingSession
        {
            EntryTime = entry,
            ExitTime = entry.AddHours(hours).AddMinutes(minutes),
            Vehicle = vehicle
        };

        var fee = _sut.Calculate(session);

        fee.Should().Be(expectedFee);
    }

    [Theory]
    [InlineData(VehicleType.Car, "2026-11-01 23:00:00", "2026-11-02 07:00:00", 30)]
    [InlineData(VehicleType.Car, "2026-11-01 22:00:00", "2026-11-01 23:00:00", 10)]
    [InlineData(VehicleType.Car, "2026-11-01 21:59:00", "2026-11-02 07:00:00", 80)]
    [InlineData(VehicleType.Car, "2026-11-01 23:00:00", "2026-11-02 08:01:00", 80)]
    [InlineData(VehicleType.Motorcycle, "2026-11-01 23:00:00", "2026-11-02 07:00:00", 15)]
    [InlineData(VehicleType.Truck, "2026-11-01 23:00:00", "2026-11-02 07:00:00", 75)]
    public void Calculate_NightWindow_AppliesLowerOfNormalFeeAndNightFlatRate(
        VehicleType vehicle, string entryStr, string exitStr, decimal expectedFee)
    {
        var session = new ParkingSession
        {
            EntryTime = DateTime.Parse(entryStr),
            ExitTime = DateTime.Parse(exitStr),
            Vehicle = vehicle
        };

        var fee = _sut.Calculate(session);

        fee.Should().Be(expectedFee);
    }

    [Theory]
    [InlineData(VehicleType.Car, 0, 5, 150)]
    [InlineData(VehicleType.Car, 30, 0, 150)]
    [InlineData(VehicleType.Truck, 30, 0, 350)]
    [InlineData(VehicleType.Motorcycle, 0, 5, 75)]
    public void Calculate_TicketLost_AppliesMinimumFeeAndBypassesFreePeriod(
        VehicleType vehicle, int hours, int minutes, decimal expectedFee)
    {
        var entry = new DateTime(2026, 11, 1, 10, 0, 0);
        var session = new ParkingSession
        {
            EntryTime = entry,
            ExitTime = entry.AddHours(hours).AddMinutes(minutes),
            Vehicle = vehicle,
            TicketLost = true
        };

        var fee = _sut.Calculate(session);

        fee.Should().Be(expectedFee);
    }

    [Theory]
    [InlineData("2026-11-06 10:00:00", 2, 0, 0)]
    [InlineData("2026-11-06 10:00:00", 2, 1, 30)]
    [InlineData("2026-11-05 10:00:00", 0, 20, 10)]
    public void Calculate_FridayEntry_ExtendsFreePeriodToTwoHours(
        string entryStr, int hours, int minutes, decimal expectedFee)
    {
        var entry = DateTime.Parse(entryStr);
        var session = new ParkingSession
        {
            EntryTime = entry,
            ExitTime = entry.AddHours(hours).AddMinutes(minutes),
            Vehicle = VehicleType.Car
        };

        var fee = _sut.Calculate(session);

        fee.Should().Be(expectedFee);
    }

    [Theory]
    [InlineData("2026-11-02 09:00:00", "2026-11-02 17:00:00", false, 0)]
    [InlineData("2026-11-02 18:00:00", "2026-11-02 20:00:00", false, 10)]
    [InlineData("2026-11-07 10:00:00", "2026-11-07 13:00:00", false, 15)]
    [InlineData("2026-11-02 10:00:00", "2026-11-02 11:00:00", true, 150)]
    public void Calculate_Subscriber_AppliesFreeWindowOrFiftyPercentDiscountAndAppliesLostTicketMinimumAfterwards(
        string entryStr, string exitStr, bool ticketLost, decimal expectedFee)
    {
        var session = new ParkingSession
        {
            EntryTime = DateTime.Parse(entryStr),
            ExitTime = DateTime.Parse(exitStr),
            Vehicle = VehicleType.Car,
            IsSubscriber = true,
            TicketLost = ticketLost
        };

        var fee = _sut.Calculate(session);

        fee.Should().Be(expectedFee);
    }
}
