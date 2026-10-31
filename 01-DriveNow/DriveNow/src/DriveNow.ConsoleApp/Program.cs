using System.Globalization;
using DriveNow.Application.Bookings;
using DriveNow.Application.Models;
using DriveNow.Application.Receipts;
using DriveNow.Application.Services;
using DriveNow.Domain.Enums;
using DriveNow.Domain.Exceptions;
using DriveNow.Domain.Services;
using DriveNow.Domain.ValueObjects;
using DriveNow.Infrastructure.External;
using DriveNow.Infrastructure.Repositories;

namespace DriveNow.ConsoleApp;

internal static class Program
{
    static readonly CustomerRepo Customers = new();
    static readonly VehicleRepository Vehicles = new();
    static readonly BookingRepository Bookings = new();
    static readonly PricingService Pricing = new();
    static readonly BookingService BookingService = new();
    static readonly ReturnService ReturnService = new();
    static readonly CancellationService CancellationService = new();

    static void Main()
    {
        PriceSeedBookings();
        while (true)
        {
            PrintMenu();
            var choice = Console.ReadLine()?.Trim();
            if (choice is null or "0")
                return;

            switch (choice)
            {
                case "1": Run(ListVehicles); break;
                case "2": Run(ListCustomers); break;
                case "3": Run(PriceQuote); break;
                case "4": Run(CreateBooking); break;
                case "5": Run(StartRental); break;
                case "6": Run(ReturnVehicle); break;
                case "7": Run(CancelBooking); break;
                case "8": Run(ListBookings); break;
                default:
                    Console.WriteLine("Unknown option.");
                    break;
            }
        }
    }

    static void PriceSeedBookings()
    {
        foreach (var booking in Bookings.GetAll())
        {
            var customer = Customers.GetById(booking.CustomerId)!;
            var vehicle = Vehicles.GetById(booking.VehicleId)!;
            booking.Price = Pricing.Calculate(customer, vehicle, new BookingRequest
            {
                CustomerId = booking.CustomerId,
                VehicleId = booking.VehicleId,
                PickupAt = booking.PickupAt,
                ReturnAt = booking.ReturnAt,
                InsurancePlan = InsurancePlan.Standard,
                AdditionalDrivers = 0,
                PromoCode = null
            });
            Bookings.Update(booking);
        }
    }

    static void PrintMenu()
    {
        Console.WriteLine();
        Console.WriteLine("DriveNow");
        Console.WriteLine("1. List vehicles");
        Console.WriteLine("2. List customers");
        Console.WriteLine("3. Price quote");
        Console.WriteLine("4. Create booking");
        Console.WriteLine("5. Start rental");
        Console.WriteLine("6. Return vehicle");
        Console.WriteLine("7. Cancel booking");
        Console.WriteLine("8. List bookings");
        Console.WriteLine("0. Exit");
        Console.Write("Option: ");
    }

    static void ListVehicles()
    {
        foreach (var vehicle in Vehicles.GetAll())
        {
            Console.WriteLine(
                $"{vehicle.Id,2}  {vehicle.PlateNumber,-10} {vehicle.Model,-16} {vehicle.Category,-10} {Money(vehicle.DailyRate),10}  {(vehicle.IsAvailable ? "available" : "unavailable")}");
        }
    }

    static void ListCustomers()
    {
        foreach (var customer in Customers.GetAll())
        {
            var age = RentalMath.CalculateAge(customer.DateOfBirth, DateTime.Today);
            Console.WriteLine(
                $"{customer.Id,2}  {customer.FullName,-16} age {age,2}  {customer.LoyaltyTier,-10} rentals {customer.CompletedRentals,2}  {(customer.IsBlacklisted ? "blacklisted" : "clear")}");
        }
    }

    static void PriceQuote()
    {
        var request = ReadRequest();
        var customer = Customers.GetById(request.CustomerId)
            ?? throw new CustomerNotFoundException($"Customer #{request.CustomerId} not found");
        var vehicle = Vehicles.GetById(request.VehicleId)
            ?? throw new VehicleNotFoundException($"Vehicle #{request.VehicleId} not found");
        PrintPrice(Pricing.Calculate(customer, vehicle, request));
    }

    static void CreateBooking()
    {
        var booking = BookingService.CreateBooking(ReadRequest());
        Console.WriteLine($"Reference: {booking.Reference}");
        Console.WriteLine($"Total: {Money(booking.Price.Total)}");
        Console.WriteLine($"Deposit: {Money(booking.Deposit)}");
    }

    static void StartRental()
    {
        BookingService.StartRental(ReadInt("Booking id: "));
        Console.WriteLine("Rental started.");
    }

    static void ReturnVehicle()
    {
        var bookingId = ReadInt("Booking id: ");
        var endMileage = ReadInt("End mileage: ");
        var fuelQuarters = ReadInt("Fuel quarters (0-4): ");
        var damageCost = ReadDecimal("Damage cost: ");
        var report = ReturnService.ProcessReturn(bookingId, endMileage, fuelQuarters, damageCost);
        PrintReport(report);
        var booking = Bookings.GetById(bookingId)!;
        var receipt = new ReceiptService(
            new LineItemFactory(),
            new TaxCalculator(),
            new CurrencyFormatter(),
            new ReceiptFormatter()).Build(booking, report);
        Console.WriteLine(receipt);
    }

    static void CancelBooking()
    {
        var fee = CancellationService.Cancel(ReadInt("Booking id: "));
        Console.WriteLine($"Fee: {Money(fee)}");
    }

    static void ListBookings()
    {
        foreach (var booking in Bookings.GetAll())
        {
            var customer = Customers.GetById(booking.CustomerId)?.FullName ?? "?";
            var vehicle = Vehicles.GetById(booking.VehicleId)?.PlateNumber ?? "?";
            Console.WriteLine(
                $"{booking.Id,2}  {booking.Reference,-16} {customer,-16} {vehicle,-10} {booking.PickupAt:yyyy-MM-dd HH:mm}  {booking.ReturnAt:yyyy-MM-dd HH:mm}  {booking.Status,-10} {Money(booking.Price.Total)}");
        }
    }

    static BookingRequest ReadRequest()
    {
        return new BookingRequest
        {
            CustomerId = ReadInt("Customer id: "),
            VehicleId = ReadInt("Vehicle id: "),
            PickupAt = ReadDate("Pickup (yyyy-MM-dd HH:mm): "),
            ReturnAt = ReadDate("Return (yyyy-MM-dd HH:mm): "),
            InsurancePlan = ReadInsurance(),
            AdditionalDrivers = ReadInt("Additional drivers: "),
            PromoCode = ReadOptional("Promo code: ")
        };
    }

    static void PrintPrice(PriceBreakdown price)
    {
        Console.WriteLine($"Rental days: {price.RentalDays}");
        Console.WriteLine($"Base: {Money(price.BaseAmount)}");
        Console.WriteLine($"Weekend surcharge: {Money(price.WeekendSurcharge)}");
        Console.WriteLine($"Young driver surcharge: {Money(price.YoungDriverSurcharge)}");
        Console.WriteLine($"Duration discount: {Money(price.DurationDiscount)}");
        Console.WriteLine($"Loyalty discount: {Money(price.LoyaltyDiscount)}");
        Console.WriteLine($"Promo discount: {Money(price.PromoDiscount)}");
        Console.WriteLine($"Insurance: {Money(price.InsuranceCost)}");
        Console.WriteLine($"Additional drivers: {Money(price.AdditionalDriversCost)}");
        Console.WriteLine($"Total: {Money(price.Total)}");
    }

    static void PrintReport(ReturnReport report)
    {
        Console.WriteLine($"Late minutes: {report.LateMinutes}");
        Console.WriteLine($"Late fee: {Money(report.LateFee)}");
        Console.WriteLine($"Allowed km: {report.AllowedKm}");
        Console.WriteLine($"Extra km: {report.ExtraKm}");
        Console.WriteLine($"Mileage fee: {Money(report.MileageFee)}");
        Console.WriteLine($"Missing fuel quarters: {report.MissingFuelQuarters}");
        Console.WriteLine($"Fuel fee: {Money(report.FuelFee)}");
        Console.WriteLine($"Damage: {Money(report.DamageCharge)}");
        Console.WriteLine($"Total extra: {Money(report.TotalExtra)}");
    }

    static void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (IsHandled(ex))
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ForegroundColor = color;
        }
    }

    static bool IsHandled(Exception ex) =>
        ex is CustomerNotFoundException
            or VehicleNotFoundException
            or BookingNotFoundException
            or BookingRejectedException
            or InvalidPromoCodeException
            or InvalidBookingStateException
            or PaymentFailedException
            or ArgumentException
            or FormatException;

    static int ReadInt(string label)
    {
        while (true)
        {
            Console.Write(label);
            if (int.TryParse(Console.ReadLine(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return value;
            Console.WriteLine("Please enter a whole number.");
        }
    }

    static decimal ReadDecimal(string label)
    {
        while (true)
        {
            Console.Write(label);
            if (decimal.TryParse(Console.ReadLine(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                return value;
            Console.WriteLine("Please enter a number.");
        }
    }

    static DateTime ReadDate(string label)
    {
        while (true)
        {
            Console.Write(label);
            if (DateTime.TryParseExact(
                    Console.ReadLine()?.Trim(),
                    "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var value))
                return value;
            Console.WriteLine("Please use yyyy-MM-dd HH:mm.");
        }
    }

    static InsurancePlan ReadInsurance()
    {
        while (true)
        {
            Console.Write("Insurance (Basic, Standard, Premium): ");
            if (Enum.TryParse<InsurancePlan>(Console.ReadLine()?.Trim(), ignoreCase: true, out var plan))
                return plan;
            Console.WriteLine("Please choose Basic, Standard or Premium.");
        }
    }

    static string? ReadOptional(string label)
    {
        Console.Write(label);
        var text = Console.ReadLine()?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    static string Money(decimal amount) =>
        amount.ToString("N2", CultureInfo.InvariantCulture);
}
