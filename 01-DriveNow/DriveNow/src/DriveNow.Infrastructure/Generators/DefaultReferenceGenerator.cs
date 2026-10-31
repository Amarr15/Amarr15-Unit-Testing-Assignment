using DriveNow.Application.Interfaces;

namespace DriveNow.Infrastructure.Generators;

public class DefaultReferenceGenerator : IReferenceGenerator
{
    public string GenerateCode()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var chars = new char[4];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = alphabet[random.Next(alphabet.Length)];

        return new string(chars);
    }
}
