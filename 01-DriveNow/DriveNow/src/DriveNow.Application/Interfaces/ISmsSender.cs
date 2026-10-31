namespace DriveNow.Application.Interfaces;

public interface ISmsSender
{
    void Send(string phone, string text);
}