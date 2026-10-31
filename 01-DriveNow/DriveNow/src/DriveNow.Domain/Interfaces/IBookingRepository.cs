using DriveNow.Domain.Entities;
namespace DriveNow.Domain.Interfaces;

public interface IBookingRepository
{
    Booking? GetById(int id);
    List<Booking> GetAll();
    void Add(Booking booking);
    void Update(Booking booking);
    List<Booking> GetOpenBookingsForVehicle(int vehicleId);
    int CountOpenBookingsForCustomer(int customerId);
}