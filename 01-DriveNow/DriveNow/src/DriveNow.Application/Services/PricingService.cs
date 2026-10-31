using DriveNow.Application.Models;
using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.Domain.Exceptions;
using DriveNow.Domain.Interfaces;
using DriveNow.Domain.Services;
using DriveNow.Domain.ValueObjects;

namespace DriveNow.Application.Services;

public class PricingService
{
    readonly IPromoCodeRepository _promos;
    readonly TimeProvider _timeProvider;

    public PricingService(IPromoCodeRepository promos, TimeProvider timeProvider)
    {
        _promos = promos;
        _timeProvider = timeProvider;
    }

    public PriceBreakdown Calculate(Customer customer, Vehicle vehicle, BookingRequest request)
    {
        var days = RentalMathService.CalculateRentalDays(request.PickupAt, request.ReturnAt);
        var baseAmount = Round(vehicle.DailyRate * days);
        var weekend = WeekendSurcharge(vehicle.DailyRate, request.PickupAt, days);
        var age = RentalMathService.CalculateAge(customer.DateOfBirth, request.PickupAt);
        var young = age is >= 21 and <= 24 ? Round(baseAmount * 0.15m) : 0m;

        var durationPercent = days >= 14 ? 15m : days >= 7 && days < 14 ? 10m : 0m;
        var loyaltyPercent = customer.LoyaltyTier switch
        {
            LoyaltyTier.Silver => 5m,
            LoyaltyTier.Gold => 10m,
            LoyaltyTier.Platinum => 15m,
            _ => 0m
        };

        var durationDiscount = Round(baseAmount * durationPercent / 100m);
        var loyaltyDiscount = Round(baseAmount * loyaltyPercent / 100m);
        var rawPromoDiscount = PromoDiscount(request.PromoCode, days, vehicle.Category, baseAmount);

        // BR-37: Discounts combine additively up to a maximum of 25% of base.
        // If combined discount exceeds 25%, keep duration and loyalty and reduce promo discount.
        var maxTotalDiscount = Round(baseAmount * 0.25m);
        var combinedFixedDiscount = durationDiscount + loyaltyDiscount;
        var promoDiscount = rawPromoDiscount;
        if (combinedFixedDiscount + promoDiscount > maxTotalDiscount)
            promoDiscount = Math.Max(0m, maxTotalDiscount - combinedFixedDiscount);

        var insurance = InsuranceCost(request.InsurancePlan, vehicle.Category, days);
        var drivers = DriverCost(customer.LoyaltyTier, request.AdditionalDrivers, days);
        var total = Round(baseAmount + weekend + young - durationDiscount - loyaltyDiscount - promoDiscount + insurance + drivers);

        return new PriceBreakdown
        {
            RentalDays = days,
            BaseAmount = baseAmount,
            WeekendSurcharge = weekend,
            YoungDriverSurcharge = young,
            DurationDiscount = durationDiscount,
            LoyaltyDiscount = loyaltyDiscount,
            PromoDiscount = promoDiscount,
            InsuranceCost = insurance,
            AdditionalDriversCost = drivers,
            Total = total
        };
    }

    static decimal WeekendSurcharge(decimal dailyRate, DateTime pickup, int days)
    {
        var weekendDays = 0;
        for (var k = 0; k < days; k++)
        {
            var start = pickup.AddHours(k * 24);
            if (start.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
                weekendDays++;
        }

        return Round(dailyRate * 0.20m * weekendDays);
    }

    decimal PromoDiscount(string? code, int days, VehicleCategory category, decimal baseAmount)
    {
        if (code is null)
            return 0m;

        var promo = _promos.GetByCode(code)
            ?? throw new InvalidPromoCodeException($"Promo code '{code}' does not exist");

        var today = _timeProvider.GetLocalNow().DateTime.Date;
        if (today < promo.ValidFrom || today > promo.ValidUntil)
            throw new InvalidPromoCodeException($"Promo code '{code}' has expired");

        if (days < promo.MinDays)
            throw new InvalidPromoCodeException($"Promo code '{code}' requires at least {promo.MinDays} days");

        if (promo.AllowedCategories is not null && !promo.AllowedCategories.Contains(category))
            throw new InvalidPromoCodeException($"Promo code '{code}' is not valid for {category}");

        if (promo.UsedCount >= promo.MaxUses)
            throw new InvalidPromoCodeException($"Promo code '{code}' has been fully used");

        return Round(baseAmount * promo.DiscountPercent / 100m);
    }

    static decimal InsuranceCost(InsurancePlan plan, VehicleCategory category, int days)
    {
        var perDay = plan switch
        {
            InsurancePlan.Standard => 150m,
            InsurancePlan.Premium => 300m,
            _ => 0m
        };
        if (category == VehicleCategory.Luxury)
            perDay *= 2;

        return Round(perDay * days);
    }

    static decimal DriverCost(LoyaltyTier tier, int additionalDrivers, int days)
    {
        var billable = additionalDrivers;
        if (tier >= LoyaltyTier.Gold && billable > 0)
            billable--;

        return Round(100m * billable * days);
    }

    static decimal Round(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
