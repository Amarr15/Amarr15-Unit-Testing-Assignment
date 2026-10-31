using Bogus;
using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;

namespace DriveNow.UnitTests.Fakers;

public class VehicleFaker : Faker<Vehicle>
{
    public VehicleFaker()
    {
        UseSeed(42);
        CustomInstantiator(f => new Vehicle
        {
            Id = f.IndexFaker + 1,
            PlateNumber = f.Random.Replace("???-####").ToUpperInvariant(),
            Model = f.Vehicle.Model(),
            Category = VehicleCategory.Economy,
            DailyRate = 600m,
            Mileage = f.Random.Number(10000, 80000),
            IsAvailable = true
        });
    }

    public VehicleFaker WithCategory(VehicleCategory category)
    {
        RuleFor(v => v.Category, _ => category);
        return this;
    }

    public VehicleFaker WithDailyRate(decimal dailyRate)
    {
        RuleFor(v => v.DailyRate, _ => dailyRate);
        return this;
    }
}
