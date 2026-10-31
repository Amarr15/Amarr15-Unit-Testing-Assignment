using DriveNow.Application.Receipts;
using DriveNow.Domain.Entities;
using DriveNow.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace DriveNow.LegacyTests;

public class ReceiptServiceTests
{
    [Fact]
    public void Build_CallsLineItemFactoryOnce()
    {
        var (sut, factory, _, _, _) = Create();

        sut.Build(new Booking(), null);

        factory.Verify(itemFactory => itemFactory.Create(It.IsAny<Booking>(), It.IsAny<ReturnReport?>()), Times.Once);
    }

    [Fact]
    public void Build_CallsTaxCalculatorOncePerLine()
    {
        var (sut, _, tax, _, _) = Create();

        sut.Build(new Booking(), null);

        tax.Verify(calculator => calculator.CalculateVat(It.IsAny<decimal>()), Times.Exactly(3));
    }

    [Fact]
    public void Build_CallsCurrencyFormatterTwicePerLinePlusTwo()
    {
        var (sut, _, _, currency, _) = Create();

        sut.Build(new Booking(), null);

        currency.Verify(formatter => formatter.Format(It.IsAny<decimal>()), Times.Exactly(8));
    }

    [Fact]
    public void Build_CallsCollaboratorsInOrder()
    {
        var items = new List<LineItem> { new(), new(), new() };
        var factory = new Mock<ILineItemFactory>(MockBehavior.Strict);
        var tax = new Mock<ITaxCalculator>(MockBehavior.Strict);
        var currency = new Mock<ICurrencyFormatter>(MockBehavior.Strict);
        var receipt = new Mock<IReceiptFormatter>(MockBehavior.Strict);
        var sequence = new MockSequence();

        factory.InSequence(sequence)
            .Setup(itemFactory => itemFactory.Create(It.IsAny<Booking>(), It.IsAny<ReturnReport?>()))
            .Returns(items);
        for (var i = 0; i < 3; i++)
            tax.InSequence(sequence).Setup(calculator => calculator.CalculateVat(It.IsAny<decimal>())).Returns(0m);
        for (var i = 0; i < 8; i++)
            currency.InSequence(sequence).Setup(formatter => formatter.Format(It.IsAny<decimal>())).Returns("X");
        receipt.InSequence(sequence)
            .Setup(formatter => formatter.Render(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns("RECEIPT");

        var sut = new ReceiptService(factory.Object, tax.Object, currency.Object, receipt.Object);
        sut.Build(new Booking(), null);
    }

    [Fact]
    public void Build_ReturnsWhatTheRendererReturns()
    {
        var (sut, _, _, _, receipt) = Create();

        var result = sut.Build(new Booking(), null);

        result.Should().Be("RECEIPT");
        receipt.Verify(formatter => formatter.Render(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>()), Times.Once);
    }

    static (ReceiptService Sut, Mock<ILineItemFactory> Factory, Mock<ITaxCalculator> Tax, Mock<ICurrencyFormatter> Currency, Mock<IReceiptFormatter> Receipt) Create()
    {
        var items = new List<LineItem>
        {
            new() { Description = "A", Amount = 10m },
            new() { Description = "B", Amount = 20m },
            new() { Description = "C", Amount = 30m }
        };
        var factory = new Mock<ILineItemFactory>();
        var tax = new Mock<ITaxCalculator>();
        var currency = new Mock<ICurrencyFormatter>();
        var receipt = new Mock<IReceiptFormatter>();
        factory.Setup(itemFactory => itemFactory.Create(It.IsAny<Booking>(), It.IsAny<ReturnReport?>())).Returns(items);
        tax.Setup(calculator => calculator.CalculateVat(It.IsAny<decimal>())).Returns(0m);
        currency.Setup(formatter => formatter.Format(It.IsAny<decimal>())).Returns("X");
        receipt.Setup(formatter => formatter.Render(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>())).Returns("RECEIPT");
        var sut = new ReceiptService(factory.Object, tax.Object, currency.Object, receipt.Object);
        return (sut, factory, tax, currency, receipt);
    }
}
