using DriveNow.Application.Models;
using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.Domain.Exceptions;
using DriveNow.Domain.Services;
using DriveNow.Infrastructure.External;
using DriveNow.Infrastructure.Logging;
using DriveNow.Infrastructure.Repositories;

namespace DriveNow.Application.Services;

public class BookingService
{
    readonly CustomerRepo _customers = new();
    readonly VehicleRepository _vehicles = new();
    readonly BookingRepository _bookings = new();
    readonly PromoCodeRepository _promos = new();
    readonly PricingService _pricing = new();
    readonly PaymentGateway _payments = new();
    readonly SmsSender _sms = new();
    readonly EmailSender _email = new();

    public Booking CreateBooking(BookingRequest request)
    {
        var customer = RequireCustomer(request.CustomerId);
        var age = RentalMath.CalculateAge(customer.DateOfBirth, request.PickupAt);
        var licenseYears = RentalMath.CalculateLicenseYears(customer.LicenseIssueDate, request.PickupAt);
        RejectCustomer(customer, age, licenseYears);

        var vehicle = _vehicles.GetById(request.VehicleId)
            ?? throw new VehicleNotFoundException($"Vehicle #{request.VehicleId} not found");
        RejectVehicle(vehicle, age, licenseYears, request);

        if (request.AdditionalDrivers is < 0 or > 3)
            throw new BookingRejectedException("Up to 3 additional drivers allowed");

        if (_bookings.GetOpenBookingsForVehicle(vehicle.Id).Any(existing =>
                request.PickupAt <= existing.ReturnAt && request.ReturnAt >= existing.PickupAt))
            throw new BookingRejectedException("Vehicle is already booked for these dates");

        if (_bookings.CountOpenBookingsForCustomer(customer.Id) >= 2)
            throw new BookingRejectedException("Customer already has 2 active bookings");

        var price = _pricing.Calculate(customer, vehicle, request);
        var booking = new Booking
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            PickupAt = request.PickupAt,
            ReturnAt = request.ReturnAt,
            InsurancePlan = request.InsurancePlan,
            AdditionalDrivers = request.AdditionalDrivers,
            PromoCode = request.PromoCode,
            Price = price,
            Status = BookingStatus.Reserved,
            CreatedAt = DateTime.Now,
            StartMileage = vehicle.Mileage,
            Reference = "DN-" + request.PickupAt.ToString("yyyyMMdd") + "-" + RandomCode()
        };

        booking.Deposit = DepositFor(vehicle.Category, request.InsurancePlan, customer.LoyaltyTier);
        if (booking.Deposit > 0)
            _payments.Charge(customer.Id, booking.Deposit, $"Deposit {booking.Reference}");

        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            var promo = _promos.GetByCode(request.PromoCode);
            if (promo is not null)
            {
                promo.UsedCount++;
                _promos.Update(promo);
            }
        }

        _bookings.Add(booking);
        var note = $"Booking {booking.Reference} confirmed";
        _sms.Send(customer.Phone, note);
        _email.Send(customer.Email, note, note);
        AuditLog.Write($"Booking {booking.Reference} created");
        return booking;
    }

    public void StartRental(int bookingId)
    {
        var booking = _bookings.GetById(bookingId)
            ?? throw new BookingNotFoundException($"Booking #{bookingId} not found");
        if (booking.Status != BookingStatus.Reserved)
            throw new InvalidBookingStateException("Only reserved bookings can be started");
        if (DateTime.Now < booking.PickupAt.AddHours(-1))
            throw new InvalidBookingStateException("Too early to start this rental");

        booking.Status = BookingStatus.Active;
        _bookings.Update(booking);
        AuditLog.Write($"Booking {booking.Reference} started");
    }

    Customer RequireCustomer(int customerId)
    {
        var customer = _customers.GetById(customerId)
            ?? throw new CustomerNotFoundException($"Customer #{customerId} not found");
        return customer;
    }

    static void RejectCustomer(Customer customer, int age, int licenseYears)
    {
        if (customer.IsBlacklisted)
            throw new BookingRejectedException("Customer is blacklisted");
        if (age < 21)
            throw new BookingRejectedException("Driver must be at least 21");
        if (licenseYears < 1)
            throw new BookingRejectedException("License must be held for at least 1 year");
    }

    static void RejectVehicle(Vehicle vehicle, int age, int licenseYears, BookingRequest request)
    {
        if (!vehicle.IsAvailable)
            throw new BookingRejectedException("Vehicle is not available");

        if (vehicle.Category == VehicleCategory.Luxury && !(age > 25 && licenseYears >= 3))
            throw new BookingRejectedException("Luxury vehicles require age 25+ and 3 years of license");

        if (vehicle.Category == VehicleCategory.Van && licenseYears < 2)
            throw new BookingRejectedException("Vans require 2 years of license");

        if (request.PickupAt < DateTime.Now.AddHours(2))
            throw new BookingRejectedException("Pickup must be at least 2 hours from now");

        if (request.ReturnAt <= request.PickupAt || request.ReturnAt - request.PickupAt > TimeSpan.FromDays(30))
            throw new BookingRejectedException("Rental period is invalid");
    }

    static decimal DepositFor(VehicleCategory category, InsurancePlan insurance, LoyaltyTier tier)
    {
        if (tier == LoyaltyTier.Platinum)
            return 0m;

        var deposit = category switch
        {
            VehicleCategory.Economy or VehicleCategory.Compact => 1000m,
            VehicleCategory.SUV or VehicleCategory.Van => 2500m,
            VehicleCategory.Luxury => 5000m,
            _ => 0m
        };
        if (insurance == InsurancePlan.Premium)
            deposit = Math.Round(deposit / 2m, 2, MidpointRounding.AwayFromZero);

        return deposit;
    }

    static string RandomCode()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var chars = new char[4];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = alphabet[random.Next(alphabet.Length)];

        return new string(chars);
    }
}
