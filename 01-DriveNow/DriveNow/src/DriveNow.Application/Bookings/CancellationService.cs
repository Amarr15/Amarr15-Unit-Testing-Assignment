using DriveNow.Domain.Enums;
using DriveNow.Domain.Exceptions;
using DriveNow.Infrastructure.External;
using DriveNow.Infrastructure.Logging;
using DriveNow.Infrastructure.Repositories;

namespace DriveNow.Application.Bookings;

public class CancellationService
{
    readonly BookingRepository _bookings = new();
    readonly VehicleRepository _vehicles = new();
    readonly CustomerRepo _customers = new();
    readonly PromoCodeRepository _promos = new();
    readonly PaymentGateway _payments = new();
    readonly SmsSender _sms = new();
    readonly EmailSender _email = new();

    public decimal Cancel(int bookingId)
    {
        var booking = _bookings.GetById(bookingId)
            ?? throw new BookingNotFoundException($"Booking #{bookingId} not found");
        if (booking.Status != BookingStatus.Reserved)
            throw new InvalidBookingStateException("Only reserved bookings can be cancelled");
        if (DateTime.Now >= booking.PickupAt)
            throw new InvalidBookingStateException("Booking has already started");

        var vehicle = _vehicles.GetById(booking.VehicleId)
            ?? throw new VehicleNotFoundException($"Vehicle #{booking.VehicleId} not found");
        var customer = _customers.GetById(booking.CustomerId)
            ?? throw new CustomerNotFoundException($"Customer #{booking.CustomerId} not found");

        var hours = (booking.PickupAt - DateTime.Now).TotalHours;
        var fee = 0m;
        if (customer.LoyaltyTier != LoyaltyTier.Platinum)
        {
            if (hours > 48)
                fee = 0m;
            else if (hours >= 24)
                fee = Round(vehicle.DailyRate * 0.50m);
            else
                fee = Round(vehicle.DailyRate);
        }

        var refund = Math.Max(booking.Deposit - fee, 0m);
        if (refund > 0)
            _payments.Refund(customer.Id, refund, $"Deposit refund {booking.Reference}");
        if (fee > booking.Deposit)
            _payments.Charge(customer.Id, fee - booking.Deposit, $"Cancellation {booking.Reference}");

        booking.Status = BookingStatus.Cancelled;
        _bookings.Update(booking);

        if (!string.IsNullOrWhiteSpace(booking.PromoCode))
        {
            var promo = _promos.GetByCode(booking.PromoCode);
            if (promo is not null)
            {
                promo.UsedCount--;
                _promos.Update(promo);
            }
        }

        var note = $"Booking {booking.Reference} cancelled";
        _sms.Send(customer.Phone, note);
        _email.Send(customer.Email, note, note);
        AuditLog.Write(note);
        return fee;
    }

    static decimal Round(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
