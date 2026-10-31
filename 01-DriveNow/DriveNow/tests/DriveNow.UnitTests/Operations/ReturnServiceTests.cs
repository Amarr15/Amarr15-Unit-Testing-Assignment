using DriveNow.Application.Interfaces;
using DriveNow.Application.Models;
using DriveNow.Application.Receipts;
using DriveNow.Application.Services;
using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.Domain.Exceptions;
using DriveNow.Domain.Interfaces;
using DriveNow.Domain.ValueObjects;
using DriveNow.UnitTests.Fakers;
using DriveNow.UnitTests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace DriveNow.UnitTests.Operations;

public class ReturnServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
    private readonly FixedTimeProvider _timeProvider = new(FixedNow);

    private ReturnService CreateSut(
        IBookingRepository? bookings = null,
        IVehicleRepository? vehicles = null,
        ICustomerRepository? customers = null,
        IPaymentGateway? payments = null,
        IEmailSender? email = null,
        IAuditLog? auditLog = null)
    {
        return new ReturnService(
            bookings ?? new FakeBookingRepository(),
            vehicles ?? new StubVehicleRepository(new VehicleFaker().Generate()),
            customers ?? new StubCustomerRepository(new CustomerFaker().Generate()),
            payments ?? new MockPaymentGateway(),
            email ?? new DummyEmailSender(),
            auditLog ?? new DummyAuditLog(),
            _timeProvider);
    }

    // BR-41: Late minutes and fee
    [Theory]
    [InlineData(0, 0)]       // On time -> 0 EGP
    [InlineData(59, 0)]      // 59m late -> 0 EGP
    [Trait("Rule", "BR-41")]
    public void ProcessReturn_LateWithinGracePeriod_ChargesNoLateFee(int lateMinutes, decimal expectedFee)
    {
        var plannedReturn = FixedNow.AddMinutes(-lateMinutes);
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

        var sut = CreateSut(
            bookings: fakeBookings,
            vehicles: new StubVehicleRepository(vehicle),
            customers: new StubCustomerRepository(customer));

        var report = sut.ProcessReturn(1, 1000, 4, 0);

        report.LateFee.Should().Be(expectedFee);
    }

    [Theory]
    [InlineData(60, 60)]     // 60m = 1 hour -> 10% of 600 = 60
    [InlineData(61, 120)]    // 61m = 2 started hours -> 20% of 600 = 120
    [InlineData(180, 180)]   // 180m = 3 started hours -> 30% of 600 = 180
    [Trait("Rule", "BR-41")]
    public void ProcessReturn_LateBetweenOneAndFourHours_ChargesHourlyRate(int lateMinutes, decimal expectedFee)
    {
        var plannedReturn = FixedNow.AddMinutes(-lateMinutes);
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

        var sut = CreateSut(
            bookings: fakeBookings,
            vehicles: new StubVehicleRepository(vehicle),
            customers: new StubCustomerRepository(customer));

        var report = sut.ProcessReturn(1, 1000, 4, 0);

        report.LateFee.Should().Be(expectedFee);
    }

    [Theory]
    [InlineData(241, 600)]    // 241m = 1 started 24h period -> 1 * 600 = 600
    [InlineData(1440, 600)]   // 1440m (24h) = 1 full period -> 600
    [InlineData(1441, 1200)]  // 1441m = 2 started periods -> 2 * 600 = 1200
    [Trait("Rule", "BR-41")]
    public void ProcessReturn_LateOverFourHours_ChargesFullDailyRatePerPeriod(int lateMinutes, decimal expectedFee)
    {
        var plannedReturn = FixedNow.AddMinutes(-lateMinutes);
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

        var sut = CreateSut(
            bookings: fakeBookings,
            vehicles: new StubVehicleRepository(vehicle),
            customers: new StubCustomerRepository(customer));

        var report = sut.ProcessReturn(1, 1000, 4, 0);

        report.LateFee.Should().Be(expectedFee);
    }

    // BR-42: Distance fee (250 km/day allowed; extra km = 0.75 EGP or 1.50 EGP for Luxury)
    [Theory]
    [InlineData(1000, 1200, 1, VehicleCategory.Economy, 0)]      // 200 km driven <= 250 allowed -> 0
    [InlineData(1000, 1350, 1, VehicleCategory.Economy, 75)]     // 100 km extra * 0.75 = 75
    [InlineData(1000, 1350, 1, VehicleCategory.Luxury, 150)]     // 100 km extra * 1.50 = 150
    [Trait("Rule", "BR-42")]
    public void ProcessReturn_ExtraMileageDriven_CalculatesDistanceFee(
        int startMileage, int endMileage, int days, VehicleCategory category, decimal expectedFee)
    {
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Active,
            ReturnAt = FixedNow,
            StartMileage = startMileage,
            Price = new PriceBreakdown { RentalDays = days }
        };
        var vehicle = new VehicleFaker().WithCategory(category).Generate();
        var customer = new CustomerFaker().Generate();
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);

        var sut = CreateSut(
            bookings: fakeBookings,
            vehicles: new StubVehicleRepository(vehicle),
            customers: new StubCustomerRepository(customer));

        var report = sut.ProcessReturn(1, endMileage, 4, 0);

        report.MileageFee.Should().Be(expectedFee);
    }

    // BR-43: Fuel fee (200 EGP per missing quarter, 300 for SUV/Van)
    [Theory]
    [InlineData(4, VehicleCategory.Economy, 0)]       // 0 missing quarters -> 0
    [InlineData(2, VehicleCategory.Economy, 400)]     // 2 missing * 200 = 400
    [InlineData(1, VehicleCategory.SUV, 900)]         // 3 missing * 300 = 900
    [Trait("Rule", "BR-43")]
    public void ProcessReturn_MissingFuelQuarters_CalculatesFuelFee(
        int fuelQuarters, VehicleCategory category, decimal expectedFee)
    {
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Active,
            ReturnAt = FixedNow,
            StartMileage = 1000,
            Price = new PriceBreakdown { RentalDays = 1 }
        };
        var vehicle = new VehicleFaker().WithCategory(category).Generate();
        var customer = new CustomerFaker().Generate();
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);

        var sut = CreateSut(
            bookings: fakeBookings,
            vehicles: new StubVehicleRepository(vehicle),
            customers: new StubCustomerRepository(customer));

        var report = sut.ProcessReturn(1, 1000, fuelQuarters, 0);

        report.FuelFee.Should().Be(expectedFee);
    }

    // BR-44: Damage capped at deductible (Basic 10,000, Standard 3,000, Premium 0)
    [Theory]
    [InlineData(InsurancePlan.Basic, 5000, 5000)]
    [InlineData(InsurancePlan.Basic, 15000, 10000)]
    [InlineData(InsurancePlan.Standard, 5000, 3000)]
    [InlineData(InsurancePlan.Premium, 5000, 0)]
    [Trait("Rule", "BR-44")]
    public void ProcessReturn_ReportedDamage_CapsChargeAtInsuranceDeductible(
        InsurancePlan plan, decimal reportedDamage, decimal expectedCharge)
    {
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Active,
            ReturnAt = FixedNow,
            InsurancePlan = plan,
            StartMileage = 1000,
            Price = new PriceBreakdown { RentalDays = 1 }
        };
        var vehicle = new VehicleFaker().Generate();
        var customer = new CustomerFaker().Generate();
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);

        var sut = CreateSut(
            bookings: fakeBookings,
            vehicles: new StubVehicleRepository(vehicle),
            customers: new StubCustomerRepository(customer));

        var report = sut.ProcessReturn(1, 1000, 4, reportedDamage);

        report.DamageCharge.Should().Be(expectedCharge);
    }

    // State validations
    [Fact]
    [Trait("Rule", "BR-46")]
    public void ProcessReturn_EndMileageBelowStart_ThrowsArgumentException()
    {
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Active,
            StartMileage = 5000
        };
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);
        var sut = CreateSut(bookings: fakeBookings);

        var act = () => sut.ProcessReturn(1, 4999, 4, 0);

        act.Should().Throw<ArgumentException>()
            .WithMessage("End mileage cannot be below the start mileage");
    }

    [Fact]
    [Trait("Rule", "BR-46")]
    public void ProcessReturn_InvalidFuelQuarters_ThrowsArgumentException()
    {
        var booking = new Booking
        {
            Id = 1,
            Status = BookingStatus.Active,
            StartMileage = 1000
        };
        var fakeBookings = new FakeBookingRepository();
        fakeBookings.Add(booking);
        var sut = CreateSut(bookings: fakeBookings);

        var act = () => sut.ProcessReturn(1, 1000, 5, 0);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Fuel quarters must be between 0 and 4");
    }
}
