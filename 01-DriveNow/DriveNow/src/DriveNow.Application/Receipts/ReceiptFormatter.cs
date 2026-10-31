namespace DriveNow.Application.Receipts;

public class ReceiptFormatter
{
    public string Render(string reference, IReadOnlyList<string> lines) =>
        $"DriveNow receipt {reference}{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
}
