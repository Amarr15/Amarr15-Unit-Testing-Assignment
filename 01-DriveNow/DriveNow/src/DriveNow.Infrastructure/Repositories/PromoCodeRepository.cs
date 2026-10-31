using DriveNow.Domain.Entities;
using DriveNow.Infrastructure.Persistence;

namespace DriveNow.Infrastructure.Repositories;

public class PromoCodeRepository
{
    public PromoCode? GetByCode(string code) =>
        InMemoryDatabase.PromoCodes.FirstOrDefault(promo =>
            string.Equals(promo.Code, code, StringComparison.OrdinalIgnoreCase));

    public void Update(PromoCode promo)
    {
        var index = InMemoryDatabase.PromoCodes.FindIndex(item =>
            string.Equals(item.Code, promo.Code, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            InMemoryDatabase.PromoCodes[index] = promo;
    }
}
