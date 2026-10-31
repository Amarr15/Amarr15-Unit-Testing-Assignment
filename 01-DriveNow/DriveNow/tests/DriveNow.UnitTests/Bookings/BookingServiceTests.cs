using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using DriveNow.Application.Interfaces;
using DriveNow.Application.Models;
using DriveNow.Application.Services;
using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.Domain.Exceptions;
using DriveNow.Domain.Interfaces;
using DriveNow.UnitTests.Customizations;
using DriveNow.UnitTests.Fakers;
using DriveNow.UnitTests.TestDoubles;
using FluentAssertions;
using Moq;
using Xunit;

namespace DriveNow.UnitTests.Bookings;

public class BookingServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private readonly FixedTimeProvider _timeProvider = new(FixedNow);
    private readonly FixedReferenceGenerator _refGen = new("ABCD");

    private BookingService CreateSut(
        ICustomerRepository? customers = null,
        IVehicleRepository? vehicles = null,
        IBookingRepository? bookings = null,
        IPromoCodeRepository? promos = null,
        PricingService? pricing = null,
        IPaymentGateway? payments = null,
        ISmsSender? sms = null,
        IEmailSender? email = null,
        IAuditLog? auditLog = null)
    {
        var promoRepo = promos ?? new FakePromoCodeRepository();
        return new BookingService(
            customers ?? new StubCustomerRepository(new CustomerFaker().Generate()),
            vehicles ?? new StubVehicleRepository(new VehicleFaker().Generate()),
            bookings ?? new FakeBookingRepository(),
            promoRepo,
            pricing ?? new PricingService(promoRepo, _timeProvider),
            payments ?? new MockPaymentGateway(),
            sms ?? new SpySmsSender(),
            email ?? new DummyEmailSender(),
            auditLog ?? new DummyAuditLog(),
            _timeProvider,
            _refGen);
    }

    // --- R1: Hand-written Test Doubles ---

    // 1. Spy & Dummy test
    [Fact]
    [Trait("Rule", "BR-27")]
    public void CreateBooking_ValidRequest_SendsConfirmationSms()
    {
        var spySms = new SpySmsSender();
        var dummyEmail = new DummyEmailSender();
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().Generate();
        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle),
            sms: spySms,
            email: dummyEmail);

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddHours(5),
            ReturnAt = FixedNow.AddDays(2),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var booking = sut.CreateBooking(request);

        spySms.SentMessages.Should().ContainSingle();
        spySms.SentMessages[0].Phone.Should().Be(customer.Phone);
        spySms.SentMessages[0].Message.Should().Contain(booking.Reference);
    }

    // 2. Stub & Fake test
    [Fact]
    [Trait("Rule", "BR-24")]
    public void CreateBooking_ValidRequest_StoresBookingInFakeRepository()
    {
        var fakeBookings = new FakeBookingRepository();
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().Generate();
        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle),
            bookings: fakeBookings);

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddHours(4),
            ReturnAt = FixedNow.AddDays(3),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var result = sut.CreateBooking(request);

        var stored = fakeBookings.GetById(result.Id);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(BookingStatus.Reserved);
        stored.Reference.Should().Be(result.Reference);
    }

    // 3. Hand-written Mock test (charges deposit)
    [Fact]
    [Trait("Rule", "BR-26")]
    public void CreateBooking_StandardCustomer_ChargesDepositViaMockGateway()
    {
        var mockPayment = new MockPaymentGateway();
        var customer = new CustomerFaker().WithTier(LoyaltyTier.None).Generate();
        var vehicle = new VehicleFaker().WithCategory(VehicleCategory.Economy).Generate(); // 1000 deposit
        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle),
            payments: mockPayment);

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddHours(3),
            ReturnAt = FixedNow.AddDays(1),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        sut.CreateBooking(request);

        mockPayment.VerifyCharged(customer.Id, 1000m);
    }

    // 4. Hand-written Mock test (Platinum customer = 0 deposit, never charged)
    [Fact]
    [Trait("Rule", "BR-26")]
    public void CreateBooking_PlatinumCustomer_NeverCallsPaymentGateway()
    {
        var mockPayment = new MockPaymentGateway();
        var customer = new CustomerFaker().WithTier(LoyaltyTier.Platinum).Generate();
        var vehicle = new VehicleFaker().WithCategory(VehicleCategory.Luxury).Generate();
        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle),
            payments: mockPayment);

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddHours(3),
            ReturnAt = FixedNow.AddDays(2),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var booking = sut.CreateBooking(request);

        booking.Deposit.Should().Be(0m);
        mockPayment.VerifyNeverCharged();
    }

    // --- R2: Moq Rewrites ---

    [Fact]
    [Trait("Rule", "BR-26")]
    public void CreateBooking_MoqStandardCustomer_ChargesDepositOnce()
    {
        var paymentMock = new Mock<IPaymentGateway>();
        var customer = new CustomerFaker().WithTier(LoyaltyTier.None).Generate();
        var vehicle = new VehicleFaker().WithCategory(VehicleCategory.Economy).Generate();
        paymentMock.Setup(p => p.Charge(customer.Id, 1000m, It.IsAny<string>())).Returns("PAY-TEST");

        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle),
            payments: paymentMock.Object);

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddHours(3),
            ReturnAt = FixedNow.AddDays(1),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        sut.CreateBooking(request);

        paymentMock.Verify(p => p.Charge(customer.Id, 1000m, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    [Trait("Rule", "BR-26")]
    public void CreateBooking_MoqPlatinumCustomer_NeverChargesDeposit()
    {
        var paymentMock = new Mock<IPaymentGateway>();
        var customer = new CustomerFaker().WithTier(LoyaltyTier.Platinum).Generate();
        var vehicle = new VehicleFaker().Generate();

        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle),
            payments: paymentMock.Object);

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddHours(3),
            ReturnAt = FixedNow.AddDays(1),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var booking = sut.CreateBooking(request);

        booking.Deposit.Should().Be(0m);
        paymentMock.Verify(p => p.Charge(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    // --- BR Business Rules Tests ---

    [Fact]
    [Trait("Rule", "BR-10")]
    public void CreateBooking_CustomerDoesNotExist_ThrowsCustomerNotFoundException()
    {
        var customerRepoMock = new Mock<ICustomerRepository>();
        customerRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Customer?)null);
        var sut = CreateSut(customers: customerRepoMock.Object);

        var request = new BookingRequest { CustomerId = 999 };

        var act = () => sut.CreateBooking(request);

        act.Should().Throw<CustomerNotFoundException>()
            .WithMessage("Customer #999 not found");
    }

    [Fact]
    [Trait("Rule", "BR-11")]
    public void CreateBooking_CustomerBlacklisted_ThrowsBookingRejectedException()
    {
        var customer = new CustomerFaker().Generate();
        customer.IsBlacklisted = true;
        var sut = CreateSut(customers: new StubCustomerRepository(customer));

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            PickupAt = FixedNow.AddHours(3),
            ReturnAt = FixedNow.AddDays(1)
        };

        var act = () => sut.CreateBooking(request);

        act.Should().Throw<BookingRejectedException>()
            .WithMessage("Customer is blacklisted");
    }

    [Fact]
    [Trait("Rule", "BR-14")]
    public void CreateBooking_VehicleDoesNotExist_ThrowsVehicleNotFoundException()
    {
        var customer = new CustomerFaker().Generate();
        var vehicleRepoMock = new Mock<IVehicleRepository>();
        vehicleRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Vehicle?)null);
        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: vehicleRepoMock.Object);

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = 404,
            PickupAt = FixedNow.AddHours(3),
            ReturnAt = FixedNow.AddDays(1)
        };

        var act = () => sut.CreateBooking(request);

        act.Should().Throw<VehicleNotFoundException>()
            .WithMessage("Vehicle #404 not found");
    }

    [Fact]
    [Trait("Rule", "BR-15")]
    public void CreateBooking_VehicleUnavailable_ThrowsBookingRejectedException()
    {
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().Generate();
        vehicle.IsAvailable = false;
        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle));

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddHours(3),
            ReturnAt = FixedNow.AddDays(1)
        };

        var act = () => sut.CreateBooking(request);

        act.Should().Throw<BookingRejectedException>()
            .WithMessage("Vehicle is not available");
    }

    [Fact]
    [Trait("Rule", "BR-18")]
    public void CreateBooking_PickupTooSoon_ThrowsBookingRejectedException()
    {
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().Generate();
        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle));

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddMinutes(90), // less than 2 hours from now
            ReturnAt = FixedNow.AddDays(1)
        };

        var act = () => sut.CreateBooking(request);

        act.Should().Throw<BookingRejectedException>()
            .WithMessage("Pickup must be at least 2 hours from now");
    }

    [Fact]
    [Trait("Rule", "BR-19")]
    public void CreateBooking_DurationOverThirtyDays_ThrowsBookingRejectedException()
    {
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().Generate();
        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle));

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddHours(3),
            ReturnAt = FixedNow.AddDays(35), // > 30 days
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var act = () => sut.CreateBooking(request);

        act.Should().Throw<BookingRejectedException>()
            .WithMessage("Rental period is invalid");
    }

    [Fact]
    [Trait("Rule", "BR-20")]
    public void CreateBooking_TooManyDrivers_ThrowsBookingRejectedException()
    {
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().Generate();
        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle));

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddHours(3),
            ReturnAt = FixedNow.AddDays(1),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 4 // > 3 drivers
        };

        var act = () => sut.CreateBooking(request);

        act.Should().Throw<BookingRejectedException>()
            .WithMessage("Up to 3 additional drivers allowed");
    }

    [Fact]
    [Trait("Rule", "BR-21")]
    public void CreateBooking_VehicleDatesOverlap_ThrowsBookingRejectedException()
    {
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().Generate();
        var fakeBookings = new FakeBookingRepository();
        var existingBooking = new Booking
        {
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddDays(2),
            ReturnAt = FixedNow.AddDays(5),
            Status = BookingStatus.Reserved
        };
        fakeBookings.Add(existingBooking);

        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle),
            bookings: fakeBookings);

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddDays(3), // overlaps 2..5
            ReturnAt = FixedNow.AddDays(4),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var act = () => sut.CreateBooking(request);

        act.Should().Throw<BookingRejectedException>()
            .WithMessage("Vehicle is already booked for these dates");
    }

    [Fact]
    [Trait("Rule", "BR-22")]
    public void CreateBooking_CustomerHasTwoActiveBookings_ThrowsBookingRejectedException()
    {
        var customer = new CustomerFaker().Generate();
        var vehicle = new VehicleFaker().Generate();
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(new Booking { CustomerId = customer.Id, Status = BookingStatus.Active });
        fakeBookings.Add(new Booking { CustomerId = customer.Id, Status = BookingStatus.Reserved });

        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle),
            bookings: fakeBookings);

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddHours(3),
            ReturnAt = FixedNow.AddDays(1),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var act = () => sut.CreateBooking(request);

        act.Should().Throw<BookingRejectedException>()
            .WithMessage("Customer already has 2 active bookings");
    }

    // --- R3: AutoFixture 1 ([Theory, AutoMoqData] with [Frozen] Mock<IPaymentGateway>) ---

    [Theory]
    [AutoMoqData]
    [Trait("Rule", "BR-26")]
    public void CreateBooking_AutoMoqFrozenGateway_ChargesExpectedDeposit(
        [Frozen] Mock<IPaymentGateway> paymentMock,
        [Frozen] Mock<ICustomerRepository> customerRepoMock,
        [Frozen] Mock<IVehicleRepository> vehicleRepoMock,
        [Frozen] Mock<IBookingRepository> bookingRepoMock,
        BookingService sut)
    {
        var pickup = DateTime.Now.AddHours(5);
        var customer = new CustomerFaker().WithTier(LoyaltyTier.None).WithAge(30, pickup).Generate();
        var vehicle = new VehicleFaker().WithCategory(VehicleCategory.Economy).Generate();

        customerRepoMock.Setup(r => r.GetById(customer.Id)).Returns(customer);
        vehicleRepoMock.Setup(r => r.GetById(vehicle.Id)).Returns(vehicle);
        bookingRepoMock.Setup(r => r.GetOpenBookingsForVehicle(It.IsAny<int>())).Returns([]);
        bookingRepoMock.Setup(r => r.CountOpenBookingsForCustomer(It.IsAny<int>())).Returns(0);

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = pickup,
            ReturnAt = pickup.AddDays(1),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        sut.CreateBooking(request);

        paymentMock.Verify(p => p.Charge(customer.Id, 1000m, It.IsAny<string>()), Times.Once);
    }

    // --- R4: AutoFixture 2 (Customization fixing random dates) ---

    [Fact]
    [Trait("Rule", "BR-12")]
    public void CreateBooking_CustomizedAdultCustomer_SucceedsWithValidDates()
    {
        var fixture = new Fixture();
        fixture.Customize(new ValidCustomerCustomization());
        var customer = fixture.Create<Customer>();
        var vehicle = new VehicleFaker().Generate();
        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle));

        var request = new BookingRequest
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = FixedNow.AddHours(5),
            ReturnAt = FixedNow.AddDays(2),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var result = sut.CreateBooking(request);

        result.Should().NotBeNull();
        result.CustomerId.Should().Be(customer.Id);
    }

    // --- R7: Verify Snapshot ---

    [Fact]
    [Trait("Rule", "BR-24")]
    public async Task CreateBooking_ValidRequest_MatchesSnapshotOutput()
    {
        var customer = new CustomerFaker()
            .WithTier(LoyaltyTier.Gold)
            .WithAge(34, FixedNow.AddDays(3))
            .Generate();
        customer.Id = 1;

        var vehicle = new VehicleFaker()
            .WithDailyRate(600m)
            .WithCategory(VehicleCategory.Economy)
            .Generate();
        vehicle.Id = 1;

        var fakeBookings = new FakeBookingRepository();
        var sut = CreateSut(
            customers: new StubCustomerRepository(customer),
            vehicles: new StubVehicleRepository(vehicle),
            bookings: fakeBookings);

        var request = new BookingRequest
        {
            CustomerId = 1,
            VehicleId = 1,
            PickupAt = FixedNow.AddDays(3),
            ReturnAt = FixedNow.AddDays(6),
            InsurancePlan = InsurancePlan.Standard,
            AdditionalDrivers = 0,
            PromoCode = null
        };

        var booking = sut.CreateBooking(request);

        await Verify(booking);
    }
}
