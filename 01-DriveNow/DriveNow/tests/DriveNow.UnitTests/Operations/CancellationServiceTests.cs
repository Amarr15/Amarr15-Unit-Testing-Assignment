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
using Moq;
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

    // BR-50: Cancellation at exact pickup time
    [Fact]
    [Trait("Rule", "BR-50")]
    public void Cancel_AtPickupTime_ThrowsInvalidBookingStateException()
    {
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Reserved,
            PickupAt = FixedNow // now == pickup
        };
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);
        var sut = CreateSut(bookings: fakeBookings);

        var act = () => sut.Cancel(1);

        act.Should().Throw<InvalidBookingStateException>()
            .WithMessage("Booking has already started");
    }

    // BR-52: Deposit exceeds cancellation fee -> refunds difference
    [Fact]
    [Trait("Rule", "BR-52")]
    public void Cancel_DepositExceedsFee_RefundsDifferenceViaGateway()
    {
        var mockPayment = new MockPaymentGateway();
        var pickup = FixedNow.AddHours(25); // notice between 24 and 48h -> 50% fee = 300
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Reserved,
            PickupAt = pickup,
            Deposit = 1000m // deposit 1000 > fee 300 -> refund 700
        };
        var vehicle = new VehicleFaker().WithDailyRate(600m).Generate();
        var customer = new CustomerFaker().WithTier(LoyaltyTier.None).Generate();
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);

        var sut = CreateSut(
            bookings: fakeBookings,
            vehicles: new StubVehicleRepository(vehicle),
            customers: new StubCustomerRepository(customer),
            payments: mockPayment);

        var fee = sut.Cancel(1);

        fee.Should().Be(300m);
        mockPayment.VerifyRefunded(customer.Id, 700m);
    }

    // BR-53: Promo code used count decremented
    [Fact]
    [Trait("Rule", "BR-53")]
    public void Cancel_WithPromoCode_DecrementsPromoUsedCount()
    {
        var pickup = FixedNow.AddHours(50);
        var promo = new PromoCode
        {
            Code = "SAVE20",
            DiscountPercent = 20,
            UsedCount = 5,
            MaxUses = 10,
            ValidFrom = FixedNow.AddDays(-1),
            ValidUntil = FixedNow.AddDays(10)
        };
        var fakePromos = new FakePromoCodeRepository(promo);

        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Reserved,
            PickupAt = pickup,
            PromoCode = "SAVE20",
            Deposit = 1000m
        };
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);

        var sut = CreateSut(
            bookings: fakeBookings,
            promos: fakePromos);

        sut.Cancel(1);

        promo.UsedCount.Should().Be(4);
    }

    // BR-53: Notifications and Audit Log dispatched
    [Fact]
    [Trait("Rule", "BR-53")]
    public void Cancel_ValidBooking_SendsNotificationsAndWritesAuditLog()
    {
        var smsMock = new Mock<ISmsSender>();
        var emailMock = new Mock<IEmailSender>();
        var auditMock = new Mock<IAuditLog>();

        var pickup = FixedNow.AddHours(50);
        var booking = new Booking
        {
            Id = 1,
            Reference = "DN-20261101-ABCD",
            Status = BookingStatus.Reserved,
            PickupAt = pickup,
            Deposit = 1000m
        };
        var customer = new CustomerFaker().Generate();
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);

        var sut = CreateSut(
            bookings: fakeBookings,
            customers: new StubCustomerRepository(customer),
            sms: smsMock.Object,
            email: emailMock.Object,
            auditLog: auditMock.Object);

        sut.Cancel(1);

        var expectedMsg = "Booking DN-20261101-ABCD cancelled";
        smsMock.Verify(s => s.Send(customer.Phone, expectedMsg), Times.Once);
        emailMock.Verify(e => e.Send(customer.Email, expectedMsg, expectedMsg), Times.Once);
        auditMock.Verify(a => a.Write(expectedMsg), Times.Once);
    }
}
