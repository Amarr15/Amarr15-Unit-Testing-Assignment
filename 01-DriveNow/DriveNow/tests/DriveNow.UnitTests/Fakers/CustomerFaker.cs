using Bogus;
using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;

namespace DriveNow.UnitTests.Fakers;

public class CustomerFaker : Faker<Customer>
{
    public CustomerFaker()
    {
        UseSeed(42);
        CustomInstantiator(f => new Customer
        {
            Id = f.IndexFaker + 1,
            FullName = f.Name.FullName(),
            Email = f.Internet.Email(),
            Phone = "010" + f.Random.ReplaceNumbers("########"),
            DateOfBirth = DateTime.Today.AddYears(-30),
            LicenseIssueDate = DateTime.Today.AddYears(-5),
            LoyaltyTier = LoyaltyTier.None,
            CompletedRentals = 0,
            IsBlacklisted = false
        });
    }

    public CustomerFaker WithAge(int age, DateTime? onDate = null)
    {
        var reference = onDate ?? DateTime.Today;
        RuleFor(c => c.DateOfBirth, _ => reference.AddYears(-age));
        return this;
    }

    public CustomerFaker WithTier(LoyaltyTier tier)
    {
        RuleFor(c => c.LoyaltyTier, _ => tier);
        return this;
    }

    public CustomerFaker WithLicenseYears(int years, DateTime? onDate = null)
    {
        var reference = onDate ?? DateTime.Today;
        RuleFor(c => c.LicenseIssueDate, _ => reference.AddYears(-years));
        return this;
    }
}
