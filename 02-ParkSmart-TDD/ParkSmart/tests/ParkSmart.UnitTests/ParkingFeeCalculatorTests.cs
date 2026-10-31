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
}
