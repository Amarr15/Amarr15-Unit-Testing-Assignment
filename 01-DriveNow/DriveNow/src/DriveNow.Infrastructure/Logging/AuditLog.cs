namespace DriveNow.Infrastructure.Logging;

public static class AuditLog
{
    public static void Write(string message)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "audit.log");
        File.AppendAllText(path, $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
    }
}
