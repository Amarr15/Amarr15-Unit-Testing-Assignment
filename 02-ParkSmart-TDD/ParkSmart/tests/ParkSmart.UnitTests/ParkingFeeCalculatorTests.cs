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
}
