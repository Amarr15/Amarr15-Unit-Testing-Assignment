using DriveNow.Application.Models;
using DriveNow.Application.Services;
using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.Domain.Exceptions;
using DriveNow.Domain.ValueObjects;
using DriveNow.UnitTests.Fakers;
using DriveNow.UnitTests.TestDoubles;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace DriveNow.UnitTests.Pricing;

public class PricingServiceTests
{
    private static readonly DateTime FixedToday = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly FixedTimeProvider _timeProvider = new(FixedToday);
    private readonly FakePromoCodeRepository _promoRepo = PromoCodeTestData.CreateTestRepository(FixedToday);

    private PricingService CreateSut() => new(_promoRepo, _timeProvider);

    // BR-30: Base = daily rate * rental days
    [Theory]
    [InlineData(1, 600, 600)]
    [InlineData(3, 600, 1800)]
    [InlineData(5, 1000, 5000)]
    [Trait("Rule", "BR-30")]
    public void Calculate_ValidDuration_ComputesBaseAmount(int days, decimal dailyRate, decimal expectedBase)
    {
        var sut = CreateSut();
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().WithDailyRate(dailyRate).Generate();
        var pickup = FixedToday.AddDays(1);
        var request = new BookingRequest
        {
            PickupAt = pickup,
            ReturnAt = pickup.AddDays(days),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var result = sut.Calculate(customer, vehicle, request);

        result.BaseAmount.Should().Be(expectedBase);
        result.RentalDays.Should().Be(days);
    }

    // BR-31: Weekend surcharge = 20% of daily rate for rental days starting Friday or Saturday
    [Theory]
    [InlineData(DayOfWeek.Wednesday, 2, 0)]     
    [InlineData(DayOfWeek.Friday, 1, 120)]       
    [InlineData(DayOfWeek.Friday, 2, 240)]       
    [InlineData(DayOfWeek.Saturday, 2, 120)]     
    [Trait("Rule", "BR-31")]
    public void Calculate_RentalIncludesWeekend_AppliesWeekendSurcharge(DayOfWeek startDay, int days, decimal expectedSurcharge)
    {
        var sut = CreateSut();
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().WithDailyRate(600m).Generate();
        
        var pickup = FixedToday;
        while (pickup.DayOfWeek != startDay)
            pickup = pickup.AddDays(1);

        var request = new BookingRequest
        {
            PickupAt = pickup,
            ReturnAt = pickup.AddDays(days),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var result = sut.Calculate(customer, vehicle, request);

        result.WeekendSurcharge.Should().Be(expectedSurcharge);
    }

    // BR-32: Young driver surcharge = 15% of base when age is 21-24 on pickup date
    [Theory]
    [InlineData(20, 0)]      // Under 21 -> 0%
    [InlineData(21, 90)]     // 21 -> 15% of 600 = 90
    [InlineData(24, 90)]     // 24 -> 15% of 600 = 90
    [InlineData(25, 0)]      // 25 -> 0%
    [Trait("Rule", "BR-32")]
    public void Calculate_DriverAgeBoundary_AppliesYoungDriverSurchargeCorrectly(int age, decimal expectedSurcharge)
    {
        var sut = CreateSut();
        var pickup = FixedToday.AddDays(2);
        var customer = new CustomerFaker().WithAge(age, pickup).Generate();
        var vehicle = new VehicleFaker().WithDailyRate(600m).Generate();
        var request = new BookingRequest
        {
            PickupAt = pickup,
            ReturnAt = pickup.AddDays(1),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var result = sut.Calculate(customer, vehicle, request);

        result.YoungDriverSurcharge.Should().Be(expectedSurcharge);
    }

    // BR-33: Duration discount = 10% for 7-13 days, 15% for 14+ days
    [Theory]
    [InlineData(6, 0)]       // 6 days -> 0%
    [InlineData(8, 480)]     // 8 days -> 10% of 4800 = 480
    [InlineData(13, 780)]    // 13 days -> 10% of 7800 = 780
    [InlineData(14, 1260)]   // 14 days -> 15% of 8400 = 1260
    [Trait("Rule", "BR-33")]
    public void Calculate_RentalDurationDays_AppliesExpectedDurationDiscount(int days, decimal expectedDiscount)
    {
        var sut = CreateSut();
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().WithDailyRate(600m).Generate();
        var pickup = FixedToday.AddDays(1);
        var request = new BookingRequest
        {
            PickupAt = pickup,
            ReturnAt = pickup.AddDays(days),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var result = sut.Calculate(customer, vehicle, request);

        result.DurationDiscount.Should().Be(expectedDiscount);
    }

    // BR-34: Loyalty discount = Silver 5%, Gold 10%, Platinum 15% of base
    [Theory]
    [InlineData(LoyaltyTier.None, 0)]
    [InlineData(LoyaltyTier.Silver, 50)]
    [InlineData(LoyaltyTier.Gold, 100)]
    [InlineData(LoyaltyTier.Platinum, 150)]
    [Trait("Rule", "BR-34")]
    public void Calculate_CustomerLoyaltyTier_AppliesExpectedLoyaltyDiscount(LoyaltyTier tier, decimal expectedDiscount)
    {
        var sut = CreateSut();
        var customer = new CustomerFaker().WithTier(tier).Generate();
        var vehicle = new VehicleFaker().WithDailyRate(1000m).Generate();
        var pickup = FixedToday.AddDays(1);
        var request = new BookingRequest
        {
            PickupAt = pickup,
            ReturnAt = pickup.AddDays(1),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var result = sut.Calculate(customer, vehicle, request);

        result.LoyaltyDiscount.Should().Be(expectedDiscount);
    }

    // BR-36: Promo code validation rules (ClassData)
    [Theory]
    [ClassData(typeof(PromoCodeTestData))]
    [Trait("Rule", "BR-36")]
    public void Calculate_InvalidPromoCode_ThrowsExpectedExceptionWithMessage(
        string promoCode, int days, VehicleCategory category, string expectedMessage)
    {
        var sut = CreateSut();
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().WithCategory(category).Generate();
        var pickup = FixedToday.AddDays(1);
        var request = new BookingRequest
        {
            PickupAt = pickup,
            ReturnAt = pickup.AddDays(days),
            PromoCode = promoCode,
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var act = () => sut.Calculate(customer, vehicle, request);

        act.Should().Throw<InvalidPromoCodeException>()
            .WithMessage(expectedMessage);
    }

    // BR-38: Insurance per day: Basic 0, Standard 150, Premium 300. Luxury pays double.
    [Theory]
    [InlineData(InsurancePlan.Basic, VehicleCategory.Economy, 2, 0)]
    [InlineData(InsurancePlan.Standard, VehicleCategory.Economy, 2, 300)]
    [InlineData(InsurancePlan.Premium, VehicleCategory.Economy, 2, 600)]
    [InlineData(InsurancePlan.Basic, VehicleCategory.Luxury, 2, 0)]
    [InlineData(InsurancePlan.Standard, VehicleCategory.Luxury, 2, 600)]
    [InlineData(InsurancePlan.Premium, VehicleCategory.Luxury, 2, 1200)]
    [Trait("Rule", "BR-38")]
    public void Calculate_InsurancePlanAndCategory_CalculatesDailyCost(
        InsurancePlan plan, VehicleCategory category, int days, decimal expectedCost)
    {
        var sut = CreateSut();
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().WithCategory(category).Generate();
        var pickup = FixedToday.AddDays(1);
        var request = new BookingRequest
        {
            PickupAt = pickup,
            ReturnAt = pickup.AddDays(days),
            InsurancePlan = plan,
            AdditionalDrivers = 0
        };

        var result = sut.Calculate(customer, vehicle, request);

        result.InsuranceCost.Should().Be(expectedCost);
    }

    // BR-39: Additional drivers cost 100 per driver per day.
    [Theory]
    [InlineData(LoyaltyTier.None, 1, 2, 200)]
    [InlineData(LoyaltyTier.None, 2, 2, 400)]
    [InlineData(LoyaltyTier.Gold, 1, 2, 0)]
    [InlineData(LoyaltyTier.Gold, 2, 2, 200)]
    [InlineData(LoyaltyTier.Platinum, 1, 2, 0)]
    [Trait("Rule", "BR-39")]
    public void Calculate_CustomerTierWithDrivers_CalculatesDriverCost(
        LoyaltyTier tier, int additionalDrivers, int days, decimal expectedCost)
    {
        var sut = CreateSut();
        var customer = new CustomerFaker().WithTier(tier).Generate();
        var vehicle = new VehicleFaker().Generate();
        var pickup = FixedToday.AddDays(1);
        var request = new BookingRequest
        {
            PickupAt = pickup,
            ReturnAt = pickup.AddDays(days),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = additionalDrivers
        };

        var result = sut.Calculate(customer, vehicle, request);

        result.AdditionalDriversCost.Should().Be(expectedCost);
    }

    // BR-40 & Worked Example A (MemberData + BeEquivalentTo + AssertionScope)
    public static IEnumerable<object[]> WorkedExampleAData()
    {
        // Example A: Vehicle 600/day, Sunday 10:00 to Wednesday 10:00 (3 days), Gold customer aged 34, Standard insurance, 0 drivers, no promo.
        var pickup = new DateTime(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc); // Sunday
        var returnAt = new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc); // Wednesday

        var customer = new CustomerFaker()
            .WithTier(LoyaltyTier.Gold)
            .WithAge(34, pickup)
            .Generate();

        var vehicle = new VehicleFaker()
            .WithDailyRate(600m)
            .WithCategory(VehicleCategory.Economy)
            .Generate();

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = pickup,
            ReturnAt = returnAt,
            InsurancePlan = InsurancePlan.Standard,
            AdditionalDrivers = 0,
            PromoCode = null
        };

        var expectedBreakdown = new PriceBreakdown
        {
            RentalDays = 3,
            BaseAmount = 1800.00m,
            WeekendSurcharge = 0.00m,
            YoungDriverSurcharge = 0.00m,
            DurationDiscount = 0.00m,
            LoyaltyDiscount = 180.00m,
            PromoDiscount = 0.00m,
            InsuranceCost = 450.00m,
            AdditionalDriversCost = 0.00m,
            Total = 2070.00m
        };

        yield return new object[] { customer, vehicle, request, expectedBreakdown };
    }

    [Theory]
    [MemberData(nameof(WorkedExampleAData))]
    [Trait("Rule", "BR-40")]
    public void Calculate_WorkedExampleA_ReturnsAccuratePriceBreakdown(
        Customer customer, Vehicle vehicle, BookingRequest request, PriceBreakdown expected)
    {
        var sut = CreateSut();

        var actual = sut.Calculate(customer, vehicle, request);

        using (new AssertionScope())
        {
            actual.Should().BeEquivalentTo(expected);
        }
    }

    [Fact]
    [Trait("Rule", "BR-40")]
    public async Task Calculate_WorkedExampleA_MatchesSnapshotOutput()
    {
        var sut = CreateSut();
        var pickup = new DateTime(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);
        var returnAt = new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);
        var customer = new CustomerFaker().WithTier(LoyaltyTier.Gold).WithAge(34, pickup).Generate();
        var vehicle = new VehicleFaker().WithDailyRate(600m).WithCategory(VehicleCategory.Economy).Generate();
        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = pickup,
            ReturnAt = returnAt,
            InsurancePlan = InsurancePlan.Standard,
            AdditionalDrivers = 0,
            PromoCode = null
        };

        var breakdown = sut.Calculate(customer, vehicle, request);

        await Verify(breakdown);
    }
}
