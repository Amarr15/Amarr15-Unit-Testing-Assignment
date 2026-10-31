using DriveNow.Application.Interfaces;

namespace DriveNow.Infrastructure.Logging;

public class AuditLog : IAuditLog
{
    public void Write(string message)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "audit.log");
        File.AppendAllText(path, $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
    }
}
