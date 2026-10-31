using DriveNow.Domain.Services;
using FluentAssertions;
using Xunit;

namespace DriveNow.UnitTests.Domain.Services;

public class RentalMathTests
{
    // BR-01: Calculate Age

    [Fact]
    [Trait("Rule", "BR-01")]
    public void CalculateAge_BirthdayHasOccurred_ReturnsCorrectAge()
    {
        // Arrange
        DateTime dateOfBirth = new DateTime(2000, 5, 15);
        DateTime onDate = new DateTime(2024, 5, 15);

        // Act
        int result = RentalMath.CalculateAge(dateOfBirth, onDate);

        // Assert
        result.Should().Be(24);
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public void CalculateAge_BirthdayHasNotOccurred_ReturnsPreviousAge()
    {
        // Arrange
        DateTime dateOfBirth = new DateTime(2000, 5, 15);
        DateTime onDate = new DateTime(2024, 5, 14);

        // Act
        int result = RentalMath.CalculateAge(dateOfBirth, onDate);

        // Assert
        result.Should().Be(23);
    }

    [Theory]
    [Trait("Rule", "BR-01")]
    [InlineData(27, 22)]
    [InlineData(28, 23)]
    public void CalculateAge_LeapYearBirthday_ReturnsCorrectAge(
        int day,
        int expectedAge)
    {
        // Arrange
        DateTime dateOfBirth = new DateTime(2000, 2, 29);
        DateTime onDate = new DateTime(2023, 2, day);

        // Act
        int result = RentalMath.CalculateAge(dateOfBirth, onDate);

        // Assert
        result.Should().Be(expectedAge);
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public void CalculateAge_TargetIsLeapYear_ReturnsAgeOnFeb29()
    {
        // Arrange
        DateTime dateOfBirth = new DateTime(2000, 2, 29);
        DateTime onDate = new DateTime(2024, 2, 29);

        // Act
        int result = RentalMath.CalculateAge(dateOfBirth, onDate);

        // Assert
        result.Should().Be(24);
    }


    // BR-02: Calculate License Years

    [Theory]
    [Trait("Rule", "BR-02")]
    [InlineData(2020, 5, 15, 4)]
    [InlineData(2020, 5, 16, 3)]
    public void CalculateLicenseYears_DifferentIssueDates_ReturnsExpectedYears(
        int year,
        int month,
        int day,
        int expectedYears)
    {
        // Arrange
        DateTime issueDate = new DateTime(year, month, day);
        DateTime onDate = new DateTime(2024, 5, 15);

        // Act
        int result = RentalMath.CalculateLicenseYears(issueDate, onDate);

        // Assert
        result.Should().Be(expectedYears);
    }


    // BR-03: Calculate Rental Days

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData(-1)]
    [InlineData(0)]
    public void CalculateRentalDays_ReturnAtIsBeforeOrEqualPickup_ThrowsArgumentException(
        int hours)
    {
        // Arrange
        DateTime pickup = new DateTime(2026, 10, 30, 10, 0, 0);
        DateTime returnAt = pickup.AddHours(hours);

        // Act
        Action act = () => RentalMath.CalculateRentalDays(pickup, returnAt);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Return must be after pickup");
    }

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData(3, 0, 1)]
    [InlineData(24, 0, 1)]
    [InlineData(24, 59, 1)]
    [InlineData(25, 1, 2)]
    [InlineData(72, 0, 3)]
    public void CalculateRentalDays_ValidDuration_ReturnsExpectedDays(
        int hours,
        int minutes,
        int expectedDays)
    {
        // Arrange
        DateTime pickup = new DateTime(2026, 10, 30, 10, 0, 0);
        DateTime returnAt = pickup.AddHours(hours).AddMinutes(minutes);

        // Act
        int result = RentalMath.CalculateRentalDays(pickup, returnAt);

        // Assert
        result.Should().Be(expectedDays);
    }
}
