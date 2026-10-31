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

    // BUG 3: BR-37 - Cap should reduce promo, not loyalty
    [Fact]
    [Trait("Rule", "BR-37")]
    public void Calculate_DiscountCapExceeded_ReducesPromoNotLoyalty()
    {
        var customer = new CustomerFaker().WithTier(LoyaltyTier.Gold).WithAge(30, FixedNow).Generate();
        var vehicle = new VehicleFaker().WithDailyRate(1000m).WithCategory(VehicleCategory.Economy).Generate();
        var promo = new PromoCode
        {
            Code = "BIG20",
            DiscountPercent = 20,
            ValidFrom = FixedNow.AddDays(-5).Date,
            ValidUntil = FixedNow.AddDays(5).Date,
            MinDays = 1,
            MaxUses = 100,
            UsedCount = 0
        };
        var promoRepo = new FakePromoCodeRepository(promo);
        var sut = new PricingService(promoRepo, _timeProvider);

        var request = new BookingRequest
        {
            PickupAt = FixedNow,
            ReturnAt = FixedNow.AddDays(3), // 3 days -> 0% duration discount
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0,
            PromoCode = "BIG20"
        };

        var result = sut.Calculate(customer, vehicle, request);

        // Base = 3000. Loyalty = 10% (300). Raw promo = 20% (600). Total raw = 30% > 25%.
        // BR-37: Keep loyalty (300), reduce promo so total discount <= 25% (max 750). Promo should be 450.
        result.LoyaltyDiscount.Should().Be(300m);
        result.PromoDiscount.Should().Be(450m);
    }

    // BUG 4: BR-39 - Free driver should be Gold/Platinum only, not Silver
    [Fact]
    [Trait("Rule", "BR-39")]
    public void Calculate_SilverCustomerWithOneDriver_DoesNotGetFreeDriver()
    {
        var customer = new CustomerFaker().WithTier(LoyaltyTier.Silver).WithAge(30, FixedNow).Generate();
        var vehicle = new VehicleFaker().WithDailyRate(500m).WithCategory(VehicleCategory.Economy).Generate();
        var promoRepo = new FakePromoCodeRepository();
        var sut = new PricingService(promoRepo, _timeProvider);

        var request = new BookingRequest
        {
            PickupAt = FixedNow,
            ReturnAt = FixedNow.AddDays(1),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 1
        };

        var result = sut.Calculate(customer, vehicle, request);

        // BR-39: Only Gold and Platinum get first driver free. Silver should pay full.
        result.AdditionalDriversCost.Should().Be(100m); // 1 driver * 100 * 1 day
    }

    // BUG 5: BR-21 - Overlap check uses <= and >= instead of < and >
    [Fact]
    [Trait("Rule", "BR-21")]
    public void CreateBooking_BackToBackRental_ShouldBeAllowed()
    {
        var existingReturn = FixedNow.AddDays(3);
        var customer = new CustomerFaker()
            .WithAge(30, FixedNow).WithLicenseYears(5, FixedNow)
            .WithTier(LoyaltyTier.None).Generate();
        customer.IsBlacklisted = false;
        var vehicle = new VehicleFaker().WithCategory(VehicleCategory.Economy).Generate();
        vehicle.IsAvailable = true;

        var existingBooking = new Booking
        {
            Id = 99,
            CustomerId = customer.Id + 1,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow,
            ReturnAt = existingReturn,
            Status = BookingStatus.Reserved
        };

        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(existingBooking);

        var promoRepo = new FakePromoCodeRepository();
        var pricing = new PricingService(promoRepo, _timeProvider);
        var sut = new BookingService(
            new StubCustomerRepository(customer),
            new StubVehicleRepository(vehicle),
            fakeBookings, promoRepo, pricing,
            new MockPaymentGateway(),
            new SpySmsSender(),
            new DummyEmailSender(),
            new DummyAuditLog(),
            _timeProvider,
            new FixedReferenceGenerator());

        // New booking starts exactly when existing ends (back-to-back)
        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = existingReturn, // pickup == existing return
            ReturnAt = existingReturn.AddDays(2),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var act = () => sut.CreateBooking(request);

        // BR-21: Back-to-back (pickup == existing return) should be allowed
        act.Should().NotThrow();
    }

    // BUG 6: BR-41 - Late fee uses >= 240 instead of > 240
    [Fact]
    [Trait("Rule", "BR-41")]
    public void ProcessReturn_ExactlyTwoHundredFortyMinutesLate_ChargesHourlyNotDaily()
    {
        var plannedReturn = FixedNow.AddMinutes(-240);
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Active,
            ReturnAt = plannedReturn,
            StartMileage = 1000,
            Price = new PriceBreakdown { RentalDays = 1 }
        };
        var vehicle = new VehicleFaker().WithDailyRate(600m).Generate();
        var customer = new CustomerFaker().Generate();
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);

        var sut = new ReturnService(
            fakeBookings,
            new StubVehicleRepository(vehicle),
            new StubCustomerRepository(customer),
            new MockPaymentGateway(),
            new DummyEmailSender(),
            new DummyAuditLog(),
            _timeProvider);

        var report = sut.ProcessReturn(1, 1000, 4, 0);

        // BR-41: 240m = exactly 4 hours -> 10% * 4 = 40% of daily (240). Not full daily (600).
        report.LateFee.Should().Be(240m);
    }

    // BUG 7: BR-51 - Cancellation uses > 48 instead of >= 48
    [Fact]
    [Trait("Rule", "BR-51")]
    public void Cancel_ExactlyFortyEightHoursNotice_ShouldChargeZeroFee()
    {
        var pickup = FixedNow.AddHours(48); // exactly 48 hours notice
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Reserved,
            PickupAt = pickup,
            Deposit = 1000m
        };
        var vehicle = new VehicleFaker().WithDailyRate(600m).Generate();
        var customer = new CustomerFaker().WithTier(LoyaltyTier.None).Generate();
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);

        var sut = new CancellationService(
            fakeBookings,
            new StubVehicleRepository(vehicle),
            new StubCustomerRepository(customer),
            new FakePromoCodeRepository(),
            new MockPaymentGateway(),
            new SpySmsSender(),
            new DummyEmailSender(),
            new DummyAuditLog(),
            _timeProvider);

        var fee = sut.Cancel(1);

        // BR-51: >= 48 hours notice -> 0 fee
        fee.Should().Be(0m);
    }
}
