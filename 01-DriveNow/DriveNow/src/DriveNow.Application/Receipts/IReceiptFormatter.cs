namespace DriveNow.Application.Receipts;

public interface IReceiptFormatter
{
    string Render(string reference, IReadOnlyList<string> lines);
}
