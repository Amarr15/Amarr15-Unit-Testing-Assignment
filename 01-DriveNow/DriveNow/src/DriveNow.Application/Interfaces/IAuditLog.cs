namespace DriveNow.Application.Interfaces;

public interface IAuditLog
{
    void Write(string message);
}