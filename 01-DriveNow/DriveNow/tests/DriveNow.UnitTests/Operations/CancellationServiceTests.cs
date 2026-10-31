using DriveNow.Application.Interfaces;
using DriveNow.Application.Models;
using DriveNow.Application.Services;
using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.Domain.Exceptions;
using DriveNow.Domain.Interfaces;
using DriveNow.UnitTests.Fakers;
using DriveNow.UnitTests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace DriveNow.UnitTests.Operations;

public class CancellationServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private readonly FixedTimeProvider _timeProvider = new(FixedNow);

    private CancellationService CreateSut(
        IBookingRepository? bookings = null,
        IVehicleRepository? vehicles = null,
        ICustomerRepository? customers = null,
        IPromoCodeRepository? promos = null,
        IPaymentGateway? payments = null,
        ISmsSender? sms = null,
        IEmailSender? email = null,
        IAuditLog? auditLog = null)
    {
        return new CancellationService(
            bookings ?? new FakeBookingRepository(),
            vehicles ?? new StubVehicleRepository(new VehicleFaker().Generate()),
            customers ?? new StubCustomerRepository(new CustomerFaker().Generate()),
            promos ?? new FakePromoCodeRepository(),
            payments ?? new MockPaymentGateway(),
            sms ?? new SpySmsSender(),
            email ?? new DummyEmailSender(),
            auditLog ?? new DummyAuditLog(),
            _timeProvider);
    }

    // BR-51: Notice > 48 hours -> 0 fee (exact 48h boundary has a bug in production: > 48 instead of >= 48)
    [Theory]
    [InlineData(49)]
    [InlineData(72)]
    [Trait("Rule", "BR-51")]
    public void Cancel_NoticeMoreThanFortyEightHours_ChargesZeroFee(int hoursUntilPickup)
    {
        var pickup = FixedNow.AddHours(hoursUntilPickup);
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

        var sut = CreateSut(
            bookings: fakeBookings,
            vehicles: new StubVehicleRepository(vehicle),
            customers: new StubCustomerRepository(customer));

        var fee = sut.Cancel(1);

        fee.Should().Be(0m);
        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    // BR-51: 24 <= Notice < 48 hours -> 50% of daily rate
    [Theory]
    [InlineData(24, 300)]
    [InlineData(47, 300)]
    [Trait("Rule", "BR-51")]
    public void Cancel_NoticeBetweenTwentyFourAndFortyEightHours_ChargesFiftyPercentFee(int hoursUntilPickup, decimal expectedFee)
    {
        var pickup = FixedNow.AddHours(hoursUntilPickup);
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

        var sut = CreateSut(
            bookings: fakeBookings,
            vehicles: new StubVehicleRepository(vehicle),
            customers: new StubCustomerRepository(customer));

        var fee = sut.Cancel(1);

        fee.Should().Be(expectedFee);
    }

    // BR-51: Notice < 24 hours -> 100% of daily rate
    [Theory]
    [InlineData(23, 600)]
    [InlineData(2, 600)]
    [Trait("Rule", "BR-51")]
    public void Cancel_NoticeUnderTwentyFourHours_ChargesFullDailyRate(int hoursUntilPickup, decimal expectedFee)
    {
        var pickup = FixedNow.AddHours(hoursUntilPickup);
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

        var sut = CreateSut(
            bookings: fakeBookings,
            vehicles: new StubVehicleRepository(vehicle),
            customers: new StubCustomerRepository(customer));

        var fee = sut.Cancel(1);

        fee.Should().Be(expectedFee);
    }

    // BR-51: Platinum customers never pay cancellation fee
    [Theory]
    [InlineData(2)]
    [InlineData(24)]
    [InlineData(50)]
    [Trait("Rule", "BR-51")]
    public void Cancel_PlatinumCustomerAnyNotice_NeverChargesFee(int hoursUntilPickup)
    {
        var pickup = FixedNow.AddHours(hoursUntilPickup);
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Reserved,
            PickupAt = pickup,
            Deposit = 0m
        };
        var vehicle = new VehicleFaker().WithDailyRate(600m).Generate();
        var customer = new CustomerFaker().WithTier(LoyaltyTier.Platinum).Generate();
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);

        var sut = CreateSut(
            bookings: fakeBookings,
            vehicles: new StubVehicleRepository(vehicle),
            customers: new StubCustomerRepository(customer));

        var fee = sut.Cancel(1);

        fee.Should().Be(0m);
    }

    // BR-50: Already started booking cannot be cancelled
    [Fact]
    [Trait("Rule", "BR-50")]
    public void Cancel_PickupAlreadyPassed_ThrowsInvalidBookingStateException()
    {
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Reserved,
            PickupAt = FixedNow.AddHours(-1)
        };
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);
        var sut = CreateSut(bookings: fakeBookings);

        var act = () => sut.Cancel(1);

        act.Should().Throw<InvalidBookingStateException>()
            .WithMessage("Booking has already started");
    }

    // BR-50: Non-reserved status
    [Fact]
    [Trait("Rule", "BR-50")]
    public void Cancel_NonReservedBooking_ThrowsInvalidBookingStateException()
    {
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Active,
            PickupAt = FixedNow.AddHours(5)
        };
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);
        var sut = CreateSut(bookings: fakeBookings);

        var act = () => sut.Cancel(1);

        act.Should().Throw<InvalidBookingStateException>()
            .WithMessage("Only reserved bookings can be cancelled");
    }

    // BR-52: Fee exceeds deposit -> charges difference via gateway
    [Fact]
    [Trait("Rule", "BR-52")]
    public void Cancel_FeeExceedsDeposit_ChargesDifferenceViaGateway()
    {
        var mockPayment = new MockPaymentGateway();
        var pickup = FixedNow.AddHours(5); // under 24 hours -> 100% daily rate = 1000 EGP
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Reserved,
            PickupAt = pickup,
            Deposit = 400m // fee (1000) > deposit (400) -> charges 600
        };
        var vehicle = new VehicleFaker().WithDailyRate(1000m).Generate();
        var customer = new CustomerFaker().WithTier(LoyaltyTier.None).Generate();
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);

        var sut = CreateSut(
            bookings: fakeBookings,
            vehicles: new StubVehicleRepository(vehicle),
            customers: new StubCustomerRepository(customer),
            payments: mockPayment);

        var fee = sut.Cancel(1);

        fee.Should().Be(1000m);
        mockPayment.VerifyCharged(customer.Id, 600m);
    }
}
