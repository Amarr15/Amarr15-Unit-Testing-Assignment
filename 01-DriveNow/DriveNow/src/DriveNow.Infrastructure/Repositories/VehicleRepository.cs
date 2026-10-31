using DriveNow.Domain.Entities;
using DriveNow.Domain.Interfaces;
using DriveNow.Infrastructure.Persistence;

namespace DriveNow.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    readonly InMemoryDatabase _db;

    public VehicleRepository(InMemoryDatabase db)
    {
        _db = db;
    }

    public Vehicle? GetById(int id) =>
        _db.Vehicles.FirstOrDefault(vehicle => vehicle.Id == id);

    public List<Vehicle> GetAll() => _db.Vehicles.ToList();

    public void Update(Vehicle vehicle)
    {
        var index = _db.Vehicles.FindIndex(item => item.Id == vehicle.Id);
        if (index >= 0)
            _db.Vehicles[index] = vehicle;
    }
}
