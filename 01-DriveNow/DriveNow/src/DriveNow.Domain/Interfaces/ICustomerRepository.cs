using DriveNow.Domain.Entities;
namespace DriveNow.Domain.Interfaces;

public interface ICustomerRepository
{
    Customer? GetById(int id);
    List<Customer> GetAll();
    void Update(Customer customer);
}