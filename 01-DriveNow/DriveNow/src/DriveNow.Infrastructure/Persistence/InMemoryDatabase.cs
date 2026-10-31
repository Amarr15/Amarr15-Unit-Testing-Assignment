using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.Domain.ValueObjects;

namespace DriveNow.Infrastructure.Persistence;

public static class InMemoryDatabase
{
    public static List<Customer> Customers = [];
    public static List<Vehicle> Vehicles = [];
    public static List<PromoCode> PromoCodes = [];
    public static List<Booking> Bookings = [];
    public static int NextBookingId = 3;

    static InMemoryDatabase()
    {
        Customers =
        [
            Customer(1, "Ahmed Fawzy", DateTime.Today.AddYears(-34), DateTime.Today.AddYears(-12), LoyaltyTier.Gold, 18, false),
            Customer(2, "Mariam Adel", DateTime.Today.AddYears(-23), DateTime.Today.AddYears(-3), LoyaltyTier.None, 2, false),
            Customer(3, "Khaled Mostafa", DateTime.Today.AddYears(-25).AddDays(-10), DateTime.Today.AddYears(-4), LoyaltyTier.Silver, 7, false),
            Customer(4, "Salma Hany", DateTime.Today.AddYears(-21).AddDays(-3), DateTime.Today.AddYears(-1).AddDays(-5), LoyaltyTier.None, 0, false),
            Customer(5, "Tamer Nabil", DateTime.Today.AddYears(-40), DateTime.Today.AddYears(-20), LoyaltyTier.Platinum, 35, false),
            Customer(6, "Rana Sherif", DateTime.Today.AddYears(-29), DateTime.Today.AddYears(-2), LoyaltyTier.Silver, 6, true),
            Customer(7, "Omar Wael", DateTime.Today.AddYears(-20), DateTime.Today.AddYears(-1), LoyaltyTier.None, 0, false),
            Customer(8, "Youssef Kamal", DateTime.Today.AddYears(-27), DateTime.Today.AddMonths(-6), LoyaltyTier.None, 0, false)
        ];

        Vehicles =
        [
            Vehicle(1, "ABC-1001", "Hyundai Accent", VehicleCategory.Economy, 600m, 42000, true),
            Vehicle(2, "ABC-1002", "Kia Rio", VehicleCategory.Economy, 650m, 18500, true),
            Vehicle(3, "CMP-2001", "Toyota Corolla", VehicleCategory.Compact, 900m, 30200, true),
            Vehicle(4, "CMP-2002", "Nissan Sunny", VehicleCategory.Compact, 850m, 77000, false),
            Vehicle(5, "SUV-3001", "Kia Sportage", VehicleCategory.SUV, 1500m, 25000, true),
            Vehicle(6, "SUV-3002", "Hyundai Tucson", VehicleCategory.SUV, 1600m, 12000, true),
            Vehicle(7, "LUX-4001", "Mercedes C200", VehicleCategory.Luxury, 4000m, 9000, true),
            Vehicle(8, "LUX-4002", "BMW 520i", VehicleCategory.Luxury, 4500m, 15500, true),
            Vehicle(9, "VAN-5001", "Toyota HiAce", VehicleCategory.Van, 2000m, 60000, true),
            Vehicle(10, "VAN-5002", "Peugeot Expert", VehicleCategory.Van, 1800m, 33000, true)
        ];

        PromoCodes =
        [
            new PromoCode
            {
                Code = "SAVE15",
                DiscountPercent = 15,
                ValidFrom = DateTime.Today.AddDays(-10),
                ValidUntil = DateTime.Today.AddDays(60),
                MinDays = 1,
                AllowedCategories = null,
                MaxUses = 100,
                UsedCount = 3
            },
            new PromoCode
            {
                Code = "WEEKLY20",
                DiscountPercent = 20,
                ValidFrom = DateTime.Today.AddDays(-5),
                ValidUntil = DateTime.Today.AddDays(90),
                MinDays = 7,
                AllowedCategories = null,
                MaxUses = 50,
                UsedCount = 0
            },
            new PromoCode
            {
                Code = "LUX10",
                DiscountPercent = 10,
                ValidFrom = DateTime.Today.AddDays(-30),
                ValidUntil = DateTime.Today.AddDays(30),
                MinDays = 2,
                AllowedCategories = [VehicleCategory.Luxury],
                MaxUses = 20,
                UsedCount = 20
            },
            new PromoCode
            {
                Code = "SUMMER25",
                DiscountPercent = 25,
                ValidFrom = DateTime.Today.AddDays(-120),
                ValidUntil = DateTime.Today.AddDays(-30),
                MinDays = 1,
                AllowedCategories = null,
                MaxUses = 100,
                UsedCount = 40
            }
        ];

        Bookings =
        [
            new Booking
            {
                Id = 1,
                Reference = "DN-SEED-0001",
                CustomerId = 1,
                VehicleId = 5,
                PickupAt = DateTime.Today.AddDays(3).AddHours(10),
                ReturnAt = DateTime.Today.AddDays(6).AddHours(10),
                InsurancePlan = InsurancePlan.Standard,
                AdditionalDrivers = 0,
                PromoCode = null,
                Price = new PriceBreakdown(),
                Deposit = 2500m,
                Status = BookingStatus.Reserved,
                CreatedAt = DateTime.Today.AddDays(-7),
                StartMileage = 25000
            },
            new Booking
            {
                Id = 2,
                Reference = "DN-SEED-0002",
                CustomerId = 5,
                VehicleId = 7,
                PickupAt = DateTime.Today.AddDays(-2).AddHours(9),
                ReturnAt = DateTime.Today.AddDays(1).AddHours(9),
                InsurancePlan = InsurancePlan.Standard,
                AdditionalDrivers = 0,
                PromoCode = null,
                Price = new PriceBreakdown(),
                Deposit = 0m,
                Status = BookingStatus.Active,
                CreatedAt = DateTime.Today.AddDays(-7),
                StartMileage = 9000
            }
        ];
    }

    static Customer Customer(
        int id,
        string fullName,
        DateTime dateOfBirth,
        DateTime licenseIssueDate,
        LoyaltyTier tier,
        int completedRentals,
        bool isBlacklisted)
    {
        var emailName = fullName.ToLowerInvariant().Replace(' ', '.');
        return new Customer
        {
            Id = id,
            FullName = fullName,
            Email = emailName + "@drivenow.eg",
            Phone = "0100000000" + id,
            DateOfBirth = dateOfBirth,
            LicenseIssueDate = licenseIssueDate,
            LoyaltyTier = tier,
            CompletedRentals = completedRentals,
            IsBlacklisted = isBlacklisted
        };
    }

    static Vehicle Vehicle(
        int id,
        string plate,
        string model,
        VehicleCategory category,
        decimal dailyRate,
        int mileage,
        bool isAvailable) => new()
    {
        Id = id,
        PlateNumber = plate,
        Model = model,
        Category = category,
        DailyRate = dailyRate,
        Mileage = mileage,
        IsAvailable = isAvailable
    };
}
