using DriveNow.Application.Models;
using DriveNow.Application.Services;
using DriveNow.Domain.Enums;
using DriveNow.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace DriveNow.UnitTests.Naive;

public class NaiveBookingServiceTests
{
    [Fact]
    public void CreateBooking_ValidRequest_CreatesBookingSuccessfully()
    {
        // Arrange
        var service = new BookingService();

        var request = new BookingRequest
        {
            CustomerId = 1,
            VehicleId = 1,
            PickupAt = DateTime.Now.AddDays(1),
            ReturnAt = DateTime.Now.AddDays(3),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        // Act
        var booking = service.CreateBooking(request);

        // Assert
        booking.Should().NotBeNull();
        booking.Status.Should().Be(BookingStatus.Reserved);
    }

    [Fact]
    public void CreateBooking_PlatinumCustomer_WaivesDeposit()
    {
        // Arrange
        var service = new BookingService();

        var request = new BookingRequest
        {
            CustomerId = 5,
            VehicleId = 2,
            PickupAt = DateTime.Now.AddDays(1),
            ReturnAt = DateTime.Now.AddDays(3),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        // Act
        var booking = service.CreateBooking(request);

        // Assert
        booking.Deposit.Should().Be(0m);
        booking.Status.Should().Be(BookingStatus.Reserved);
    }

    [Fact]
    public void CreateBooking_OverlappingDates_ThrowsBookingRejectedException()
    {
        // Arrange
        var service = new BookingService();

        DateTime pickup = DateTime.Now.AddDays(1);
        DateTime returnAt = DateTime.Now.AddDays(3);

        var firstRequest = new BookingRequest
        {
            CustomerId = 1,
            VehicleId = 1,
            PickupAt = pickup,
            ReturnAt = returnAt,
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        var secondRequest = new BookingRequest
        {
            CustomerId = 2,
            VehicleId = 1,
            PickupAt = pickup.AddDays(1),
            ReturnAt = returnAt.AddDays(1),
            InsurancePlan = InsurancePlan.Basic,
            AdditionalDrivers = 0
        };

        service.CreateBooking(firstRequest);

        // Act
        Action act = () => service.CreateBooking(secondRequest);

        // Assert
        act.Should().Throw<BookingRejectedException>()
            .WithMessage("Vehicle is already booked for these dates");
    }
}
