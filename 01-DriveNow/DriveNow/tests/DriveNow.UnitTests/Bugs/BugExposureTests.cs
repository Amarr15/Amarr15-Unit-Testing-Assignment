using DriveNow.Application.Models;
using DriveNow.Application.Services;
using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.Domain.Services;
using DriveNow.Domain.ValueObjects;
using DriveNow.UnitTests.Fakers;
using DriveNow.UnitTests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace DriveNow.UnitTests.Bugs;

public class BugExposureTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
    private readonly FixedTimeProvider _timeProvider = new(FixedNow);

    // BUG 1: BR-03 - RentalMath uses >= 60 instead of > 60 for grace hour
    [Fact]
    [Trait("Rule", "BR-03")]
    public void CalculateRentalDays_ExactlySixtyLeftoverMinutes_ShouldNotAddExtraDay()
    {
        var pickup = new DateTime(2026, 10, 30, 10, 0, 0);
        var returnAt = pickup.AddHours(25); // 1 day + exactly 60 min remainder

        var result = RentalMathService.CalculateRentalDays(pickup, returnAt);

        // BR-03: Grace period is up to 60 minutes, so exactly 60 minutes should not trigger extra day
        result.Should().Be(1);
    }

    // BUG 2: BR-33 - Duration discount: days > 7 should be days >= 7
    [Fact]
    [Trait("Rule", "BR-33")]
    public void Calculate_SevenDayRental_AppliesTenPercentDurationDiscount()
    {
        var customer = new CustomerFaker().WithTier(LoyaltyTier.None).WithAge(30, FixedNow).Generate();
        var vehicle = new VehicleFaker().WithDailyRate(1000m).WithCategory(VehicleCategory.Economy).Generate();
        var promoRepo = new FakePromoCodeRepository();
        var sut = new PricingService(promoRepo, _timeProvider);

        var request = new BookingRequest
        {
            PickupAt = FixedNow,
            ReturnAt = FixedNow.AddDays(7), // exactly 7 days
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var result = sut.Calculate(customer, vehicle, request);

        // BR-33: 7-13 days -> 10% of base. Base = 7000. Discount should be 700
        result.DurationDiscount.Should().Be(700m);
    }
}
