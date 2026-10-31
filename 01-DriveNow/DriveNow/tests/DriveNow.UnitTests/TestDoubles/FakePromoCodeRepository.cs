using DriveNow.Domain.Entities;
using DriveNow.Domain.Interfaces;

namespace DriveNow.UnitTests.TestDoubles;

public class FakePromoCodeRepository : IPromoCodeRepository
{
    private readonly List<PromoCode> _promos = new();

    public FakePromoCodeRepository(params PromoCode[] promos)
    {
        _promos.AddRange(promos);
    }

    public PromoCode? GetByCode(string code) =>
        _promos.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));

    public void Update(PromoCode promo)
    {
        var idx = _promos.FindIndex(p => string.Equals(p.Code, promo.Code, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
            _promos[idx] = promo;
        else
            _promos.Add(promo);
    }
}

public class FixedTimeProvider(DateTime utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new DateTimeOffset(utcNow, TimeSpan.Zero);
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
