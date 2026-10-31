using DriveNow.Domain.Entities;
using DriveNow.Domain.Interfaces;
using DriveNow.Infrastructure.Persistence;

namespace DriveNow.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    readonly InMemoryDatabase _db;

    public CustomerRepository(InMemoryDatabase db)
    {
        _db = db;
    }

    public Customer? GetById(int id) =>
        _db.Customers.FirstOrDefault(customer => customer.Id == id);

    public List<Customer> GetAll() => _db.Customers.ToList();

    public void Update(Customer customer)
    {
        var index = _db.Customers.FindIndex(item => item.Id == customer.Id);
        if (index >= 0)
            _db.Customers[index] = customer;
    }
}
