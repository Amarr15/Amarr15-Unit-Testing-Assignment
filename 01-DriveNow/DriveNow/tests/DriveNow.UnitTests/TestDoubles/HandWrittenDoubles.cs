using DriveNow.Application.Interfaces;
using DriveNow.Domain.Entities;
using DriveNow.Domain.Enums;
using DriveNow.Domain.Interfaces;

namespace DriveNow.UnitTests.TestDoubles;

// 1. Dummy: satisfies parameter, no behavior tested
public class DummyEmailSender : IEmailSender
{
    public void Send(string to, string subject, string body)
    {
        // Dummy does not perform real work
    }
}

public class DummyAuditLog : IAuditLog
{
    public void Write(string message)
    {
    }
}

// 2. Stub: returns canned data
public class StubCustomerRepository : ICustomerRepository
{
    private readonly Customer? _customer;

    public StubCustomerRepository(Customer? customer = null)
    {
        _customer = customer;
    }

    public Customer? GetById(int id) => _customer;
    public List<Customer> GetAll() => _customer != null ? [_customer] : [];
    public void Update(Customer customer) { }
}

public class StubVehicleRepository : IVehicleRepository
{
    private readonly Vehicle? _vehicle;

    public StubVehicleRepository(Vehicle? vehicle = null)
    {
        _vehicle = vehicle;
    }

    public Vehicle? GetById(int id) => _vehicle;
    public List<Vehicle> GetAll() => _vehicle != null ? [_vehicle] : [];
    public void Update(Vehicle vehicle) { }
}

// 3. Spy: records calls for test assertion
public class SpySmsSender : ISmsSender
{
    public List<(string Phone, string Message)> SentMessages { get; } = [];

    public void Send(string phone, string text)
    {
        SentMessages.Add((phone, text));
    }
}

// 4. Mock: hand-written verification of expectations
public class MockPaymentGateway : IPaymentGateway
{
    public int ChargeCallCount { get; private set; }
    public decimal? LastAmountCharged { get; private set; }
    public int? LastCustomerId { get; private set; }

    public string Charge(int customerId, decimal amount, string description)
    {
        ChargeCallCount++;
        LastCustomerId = customerId;
        LastAmountCharged = amount;
        return "MOCK-PAY-1234";
    }

    public int RefundCallCount { get; private set; }
    public decimal? LastAmountRefunded { get; private set; }
    public int? LastRefundCustomerId { get; private set; }

    public void Refund(int customerId, decimal amount, string description)
    {
        RefundCallCount++;
        LastRefundCustomerId = customerId;
        LastAmountRefunded = amount;
    }

    public void VerifyRefunded(int customerId, decimal expectedAmount)
    {
        if (RefundCallCount != 1)
            throw new InvalidOperationException($"Expected Refund to be called once, but called {RefundCallCount} times.");
        if (LastRefundCustomerId != customerId)
            throw new InvalidOperationException($"Expected CustomerId {customerId}, but was {LastRefundCustomerId}.");
        if (LastAmountRefunded != expectedAmount)
            throw new InvalidOperationException($"Expected Amount {expectedAmount}, but was {LastAmountRefunded}.");
    }

    public void VerifyCharged(int customerId, decimal expectedAmount)
    {
        if (ChargeCallCount != 1)
            throw new InvalidOperationException($"Expected Charge to be called once, but called {ChargeCallCount} times.");
        if (LastCustomerId != customerId)
            throw new InvalidOperationException($"Expected CustomerId {customerId}, but was {LastCustomerId}.");
        if (LastAmountCharged != expectedAmount)
            throw new InvalidOperationException($"Expected Amount {expectedAmount}, but was {LastAmountCharged}.");
    }

    public void VerifyNeverCharged()
    {
        if (ChargeCallCount != 0)
            throw new InvalidOperationException($"Expected Charge never to be called, but called {ChargeCallCount} times.");
    }
}

// 5. Fake: working in-memory implementation
public class FakeBookingRepository : IBookingRepository
{
    private readonly List<Booking> _bookings = [];
    private int _nextId = 1;

    public Booking? GetById(int id) => _bookings.FirstOrDefault(b => b.Id == id);
    public List<Booking> GetAll() => _bookings.ToList();

    public void Add(Booking booking)
    {
        booking.Id = _nextId++;
        _bookings.Add(booking);
    }

    public void Update(Booking booking)
    {
        var idx = _bookings.FindIndex(b => b.Id == booking.Id);
        if (idx >= 0)
            _bookings[idx] = booking;
    }

    public List<Booking> GetOpenBookingsForVehicle(int vehicleId) =>
        _bookings.Where(b => b.VehicleId == vehicleId && b.Status is BookingStatus.Reserved or BookingStatus.Active).ToList();

    public int CountOpenBookingsForCustomer(int customerId) =>
        _bookings.Count(b => b.CustomerId == customerId && b.Status is BookingStatus.Reserved or BookingStatus.Active);
}

public class FixedReferenceGenerator(string code = "FIXED1") : IReferenceGenerator
{
    public string GenerateCode() => code;
}
