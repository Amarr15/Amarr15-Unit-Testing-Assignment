using System.Collections;
using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.UnitTests.TestDoubles;

namespace DriveNow.UnitTests.Pricing;

public class PromoCodeTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        // 1. Not found
        yield return new object[]
        {
            "INVALID",
            3,
            VehicleCategory.Economy,
            "Promo code 'INVALID' does not exist"
        };

        // 2. Expired (today > ValidUntil or today < ValidFrom)
        yield return new object[]
        {
            "EXPIRED",
            3,
            VehicleCategory.Economy,
            "Promo code 'EXPIRED' has expired"
        };

        // 3. Too few days
        yield return new object[]
        {
            "WEEKLY20",
            4, // MinDays is 7
            VehicleCategory.Economy,
            "Promo code 'WEEKLY20' requires at least 7 days"
        };

        // 4. Wrong category
        yield return new object[]
        {
            "LUX10",
            3,
            VehicleCategory.Economy, // Allowed is Luxury only
            "Promo code 'LUX10' is not valid for Economy"
        };

        // 5. Max uses reached
        yield return new object[]
        {
            "MAXEDOUT",
            3,
            VehicleCategory.Economy,
            "Promo code 'MAXEDOUT' has been fully used"
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static FakePromoCodeRepository CreateTestRepository(DateTime today)
    {
        return new FakePromoCodeRepository(
            new PromoCode
            {
                Code = "SAVE15",
                DiscountPercent = 15,
                ValidFrom = today.AddDays(-10),
                ValidUntil = today.AddDays(30),
                MinDays = 1,
                AllowedCategories = null,
                MaxUses = 100,
                UsedCount = 5
            },
            new PromoCode
            {
                Code = "EXPIRED",
                DiscountPercent = 10,
                ValidFrom = today.AddDays(-50),
                ValidUntil = today.AddDays(-10),
                MinDays = 1,
                AllowedCategories = null,
                MaxUses = 100,
                UsedCount = 0
            },
            new PromoCode
            {
                Code = "WEEKLY20",
                DiscountPercent = 20,
                ValidFrom = today.AddDays(-10),
                ValidUntil = today.AddDays(30),
                MinDays = 7,
                AllowedCategories = null,
                MaxUses = 100,
                UsedCount = 0
            },
            new PromoCode
            {
                Code = "LUX10",
                DiscountPercent = 10,
                ValidFrom = today.AddDays(-10),
                ValidUntil = today.AddDays(30),
                MinDays = 2,
                AllowedCategories = [VehicleCategory.Luxury],
                MaxUses = 100,
                UsedCount = 0
            },
            new PromoCode
            {
                Code = "MAXEDOUT",
                DiscountPercent = 10,
                ValidFrom = today.AddDays(-10),
                ValidUntil = today.AddDays(30),
                MinDays = 1,
                AllowedCategories = null,
                MaxUses = 5,
                UsedCount = 5
            }
        );
    }
}
