using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;

namespace DriveNow.Domain.Services;

public static class LoyaltyService
{
    public static void UpgradeTier(Customer customer)
    {
        var target = LoyaltyTier.None;
        if (customer.CompletedRentals >= 30)
            target = LoyaltyTier.Platinum;
        else if (customer.CompletedRentals >= 15)
            target = LoyaltyTier.Gold;
        else if (customer.CompletedRentals >= 5)
            target = LoyaltyTier.Silver;

        customer.LoyaltyTier = target;
    }
}
