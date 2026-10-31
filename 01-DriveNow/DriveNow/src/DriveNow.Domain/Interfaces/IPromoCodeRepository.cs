using DriveNow.Domain.Entities;
namespace DriveNow.Domain.Interfaces;

public interface IPromoCodeRepository
{
    PromoCode? GetByCode(string code);
    void Update(PromoCode promo);
}