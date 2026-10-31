using DriveNow.Domain.Entities;
using DriveNow.Domain.Interfaces;
using DriveNow.Infrastructure.Persistence;

namespace DriveNow.Infrastructure.Repositories;

public class PromoCodeRepository : IPromoCodeRepository
{
    readonly InMemoryDatabase _db;

    public PromoCodeRepository(InMemoryDatabase db)
    {
        _db = db;
    }

    public PromoCode? GetByCode(string code) =>
        _db.PromoCodes.FirstOrDefault(promo =>
            string.Equals(promo.Code, code, StringComparison.OrdinalIgnoreCase));

    public void Update(PromoCode promo)
    {
        var index = _db.PromoCodes.FindIndex(item =>
            string.Equals(item.Code, promo.Code, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            _db.PromoCodes[index] = promo;
    }
}
