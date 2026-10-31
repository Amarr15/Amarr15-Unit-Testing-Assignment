namespace DriveNow.Infrastructure.External;

public class EmailSender
{
    public void Send(string to, string subject, string body)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "outbox");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd_HHmmssfff}_{to}.txt");
        var content = $"To: {to}{Environment.NewLine}Subject: {subject}{Environment.NewLine}{Environment.NewLine}{body}";
        File.WriteAllText(path, content);
    }
}
