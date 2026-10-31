using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.Infrastructure.Persistence;

namespace DriveNow.Infrastructure.Repositories;

public class BookingRepository
{
    public Booking? GetById(int id) =>
        InMemoryDatabase.Bookings.FirstOrDefault(booking => booking.Id == id);

    public List<Booking> GetAll() => InMemoryDatabase.Bookings.ToList();

    public void Add(Booking booking)
    {
        booking.Id = InMemoryDatabase.NextBookingId++;
        InMemoryDatabase.Bookings.Add(booking);
    }

    public void Update(Booking booking)
    {
        var index = InMemoryDatabase.Bookings.FindIndex(item => item.Id == booking.Id);
        if (index >= 0)
            InMemoryDatabase.Bookings[index] = booking;
    }

    public List<Booking> GetOpenBookingsForVehicle(int vehicleId) =>
        InMemoryDatabase.Bookings
            .Where(booking => booking.VehicleId == vehicleId
                && booking.Status is BookingStatus.Reserved or BookingStatus.Active)
            .ToList();

    public int CountOpenBookingsForCustomer(int customerId) =>
        InMemoryDatabase.Bookings.Count(booking => booking.CustomerId == customerId
            && booking.Status is BookingStatus.Reserved or BookingStatus.Active);
}
