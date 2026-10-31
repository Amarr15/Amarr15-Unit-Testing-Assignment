using DriveNow.Domain.Entities;
using DriveNow.Infrastructure.Persistence;

namespace DriveNow.Infrastructure.Repositories;

public class VehicleRepository
{
    public Vehicle? GetById(int id) =>
        InMemoryDatabase.Vehicles.FirstOrDefault(vehicle => vehicle.Id == id);

    public List<Vehicle> GetAll() => InMemoryDatabase.Vehicles.ToList();

    public void Update(Vehicle vehicle)
    {
        var index = InMemoryDatabase.Vehicles.FindIndex(item => item.Id == vehicle.Id);
        if (index >= 0)
            InMemoryDatabase.Vehicles[index] = vehicle;
    }
}
