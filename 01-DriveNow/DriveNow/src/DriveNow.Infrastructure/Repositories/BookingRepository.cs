using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.Domain.Interfaces;
using DriveNow.Infrastructure.Persistence;

namespace DriveNow.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    readonly InMemoryDatabase _db;

    public BookingRepository(InMemoryDatabase db)
    {
        _db = db;
    }

    public Booking? GetById(int id) =>
        _db.Bookings.FirstOrDefault(booking => booking.Id == id);

    public List<Booking> GetAll() => _db.Bookings.ToList();

    public void Add(Booking booking)
    {
        booking.Id = _db.NextBookingId++;
        _db.Bookings.Add(booking);
    }

    public void Update(Booking booking)
    {
        var index = _db.Bookings.FindIndex(item => item.Id == booking.Id);
        if (index >= 0)
            _db.Bookings[index] = booking;
    }

    public List<Booking> GetOpenBookingsForVehicle(int vehicleId) =>
        _db.Bookings
            .Where(booking => booking.VehicleId == vehicleId
                && booking.Status is BookingStatus.Reserved or BookingStatus.Active)
            .ToList();

    public int CountOpenBookingsForCustomer(int customerId) =>
        _db.Bookings.Count(booking => booking.CustomerId == customerId
            && booking.Status is BookingStatus.Reserved or BookingStatus.Active);
}
