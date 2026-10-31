using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;

namespace DriveNow.UnitTests.Customizations;

public class AutoMoqDataAttribute : AutoDataAttribute
{
    public AutoMoqDataAttribute() : base(() =>
    {
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization { ConfigureMembers = true });
        return fixture;
    })
    {
    }
}

public class ValidCustomerCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<Customer>(composer => composer
            .With(c => c.DateOfBirth, DateTime.Today.AddYears(-30))
            .With(c => c.LicenseIssueDate, DateTime.Today.AddYears(-5))
            .With(c => c.IsBlacklisted, false)
            .With(c => c.LoyaltyTier, LoyaltyTier.None));
    }
}
