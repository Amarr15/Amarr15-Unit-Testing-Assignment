using DriveNow.Application.Receipts;
using DriveNow.Application.Services;
using DriveNow.Domain.Entities;
using DriveNow.Domain.ValueObjects;
using FluentAssertions;

namespace DriveNow.LegacyTests;

public class ReceiptServiceTests
{
    [Fact]
    public async Task Build_CompletedRentalWithLateFeeAndDamage_MatchesSnapshot()
    {
        // Arrange: Real collaborators, no mocks
        var sut = new ReceiptService(
            new LineItemFactory(),
            new TaxCalculator(),
            new CurrencyFormatter(),
            new ReceiptFormatter());

        var booking = new Booking
        {
            Id = 1,
            Reference = "DN-20261105-ABCD",
            Price = new PriceBreakdown
            {
                RentalDays = 3,
                BaseAmount = 1800m,
                WeekendSurcharge = 360m,
                InsuranceCost = 300m,
                Total = 2460m
            }
        };

        var report = new ReturnReport
        {
            LateMinutes = 120,
            LateFee = 120m,
            DamageCharge = 500m
        };

        // Act: Generate the receipt
        var receipt = sut.Build(booking, report);

        // Assert: Snapshot the entire rendered receipt output
        await Verify(receipt);
    }

    [Fact]
    public void Build_ValidLineItems_CalculatesAccurateVatAndTotal()
    {
        // Arrange
        var sut = new ReceiptService(
            new LineItemFactory(),
            new TaxCalculator(),
            new CurrencyFormatter(),
            new ReceiptFormatter());

        var booking = new Booking
        {
            Reference = "DN-TEST-001",
            Price = new PriceBreakdown { BaseAmount = 1000m }
        };

        // Act
        var receipt = sut.Build(booking, null);

        // Assert: Validates that real VAT (14%) and total are accurately computed
        receipt.Should().Contain("Base rental...................1,000.00 EGP");
        receipt.Should().Contain("VAT 14%.......................  140.00 EGP");
        receipt.Should().Contain("Total.........................1,140.00 EGP");
    }
}
