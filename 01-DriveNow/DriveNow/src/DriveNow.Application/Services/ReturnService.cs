using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.Domain.Exceptions;
using DriveNow.Domain.Services;
using DriveNow.Domain.ValueObjects;
using DriveNow.Application.Receipts;
using DriveNow.Infrastructure.External;
using DriveNow.Infrastructure.Logging;
using DriveNow.Infrastructure.Repositories;

namespace DriveNow.Application.Services;

public class ReturnService
{
    readonly BookingRepository _bookings = new();
    readonly VehicleRepository _vehicles = new();
    readonly CustomerRepo _customers = new();
    readonly PaymentGateway _payments = new();
    readonly EmailSender _email = new();

    public ReturnReport ProcessReturn(int bookingId, int endMileage, int fuelQuarters, decimal damageCost)
    {
        var booking = _bookings.GetById(bookingId)
            ?? throw new BookingNotFoundException($"Booking #{bookingId} not found");
        if (booking.Status != BookingStatus.Active)
            throw new InvalidBookingStateException("Only active bookings can be returned");
        if (endMileage < booking.StartMileage)
            throw new ArgumentException("End mileage cannot be below the start mileage");
        if (fuelQuarters is < 0 or > 4)
            throw new ArgumentException("Fuel quarters must be between 0 and 4");

        var vehicle = _vehicles.GetById(booking.VehicleId)
            ?? throw new VehicleNotFoundException($"Vehicle #{booking.VehicleId} not found");
        var customer = _customers.GetById(booking.CustomerId)
            ?? throw new CustomerNotFoundException($"Customer #{booking.CustomerId} not found");

        var now = DateTime.Now;
        var report = BuildReport(booking, vehicle, endMileage, fuelQuarters, damageCost, now);

        booking.Status = BookingStatus.Completed;
        booking.ActualReturnAt = now;
        booking.EndMileage = endMileage;
        booking.ExtraCharges = report.TotalExtra;
        _bookings.Update(booking);

        vehicle.Mileage = endMileage;
        _vehicles.Update(vehicle);

        customer.CompletedRentals++;
        LoyaltyService.UpgradeTier(customer);
        _customers.Update(customer);

        if (report.TotalExtra > 0)
            _payments.Charge(customer.Id, report.TotalExtra, $"Extras {booking.Reference}");
        if (booking.Deposit > 0)
            _payments.Refund(customer.Id, booking.Deposit, $"Deposit refund {booking.Reference}");

        var receipt = new ReceiptService(
            new LineItemFactory(),
            new TaxCalculator(),
            new CurrencyFormatter(),
            new ReceiptFormatter()).Build(booking, report);
        _email.Send(customer.Email, $"Receipt {booking.Reference}", receipt);
        AuditLog.Write($"Booking {booking.Reference} returned");
        return report;
    }

    static ReturnReport BuildReport(
        Booking booking,
        Vehicle vehicle,
        int endMileage,
        int fuelQuarters,
        decimal damageCost,
        DateTime now)
    {
        var lateMinutes = now <= booking.ReturnAt
            ? 0
            : (int)Math.Floor((now - booking.ReturnAt).TotalMinutes);
        var lateFee = LateFee(lateMinutes, vehicle.DailyRate);
        var allowedKm = 250 * booking.Price.RentalDays;
        var driven = endMileage - booking.StartMileage;
        var extraKm = Math.Max(0, driven - allowedKm);
        var perKm = vehicle.Category == VehicleCategory.Luxury ? 1.50m : 0.75m;
        var mileageFee = Round(extraKm * perKm);
        var missingFuel = 4 - fuelQuarters;
        var perQuarter = vehicle.Category is VehicleCategory.SUV or VehicleCategory.Van ? 300m : 200m;
        var fuelFee = Round(missingFuel * perQuarter);
        var deductible = booking.InsurancePlan switch
        {
            InsurancePlan.Basic => 10000m,
            InsurancePlan.Standard => 3000m,
            _ => 0m
        };
        var damageCharge = Round(Math.Min(damageCost, deductible));

        return new ReturnReport
        {
            LateMinutes = lateMinutes,
            LateFee = lateFee,
            AllowedKm = allowedKm,
            ExtraKm = extraKm,
            MileageFee = mileageFee,
            MissingFuelQuarters = missingFuel,
            FuelFee = fuelFee,
            DamageCharge = damageCharge,
            TotalExtra = Round(lateFee + mileageFee + fuelFee + damageCharge)
        };
    }

    static decimal LateFee(int lateMinutes, decimal dailyRate)
    {
        if (lateMinutes >= 240)
        {
            var periods = (int)Math.Ceiling(lateMinutes / 1440d);
            return Round(dailyRate * Math.Max(periods, 1));
        }

        if (lateMinutes >= 60)
        {
            var startedHours = lateMinutes % 60 == 0 ? lateMinutes / 60 : lateMinutes / 60 + 1;
            return Round(dailyRate * 0.10m * startedHours);
        }

        return 0m;
    }

    static decimal Round(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
