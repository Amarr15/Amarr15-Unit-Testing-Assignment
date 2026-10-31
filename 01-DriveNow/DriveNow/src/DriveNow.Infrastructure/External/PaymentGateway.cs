namespace DriveNow.Infrastructure.External;

public class PaymentGateway
{
    public string Charge(int customerId, decimal amount, string description)
    {
        PauseAndMaybeFail();
        Console.WriteLine($"[Payment] Charged {amount:N2} EGP to customer #{customerId}");
        return "PAY-" + RandomCode();
    }

    public void Refund(int customerId, decimal amount, string description)
    {
        PauseAndMaybeFail();
        Console.WriteLine($"[Payment] Refunded {amount:N2} EGP to customer #{customerId}");
    }

    static void PauseAndMaybeFail()
    {
        Thread.Sleep(1500);
        if (new Random().Next(10) == 0)
            throw new PaymentFailedException("Gateway timeout");
    }

    static string RandomCode()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = alphabet[random.Next(alphabet.Length)];

        return new string(chars);
    }
}
