namespace DriveNow.Domain.ValueObjects;

public class LineItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
