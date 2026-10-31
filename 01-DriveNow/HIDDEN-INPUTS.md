# Hidden Inputs Inventory (Task 4)
- **Name**: Ammar Yasser
- **Group / ID**: Backend .NET - Cycle 1

| Class | Line | Hidden Input | Why It Hurts a Test | How You Removed It |
|---|---|---|---|---|
| `BookingService` | 14-22 | `new CustomerRepo()`, `new PaymentGateway()`, etc. | Hardcoded concrete dependencies; impossible to mock or isolate in tests | Injected interfaces via constructor |
| `BookingService` | 56, 89, 125 | `DateTime.Now` | Clock changes each run; cannot test time boundaries reliably | Injected `TimeProvider` |
| `BookingService` | 58, 150-159 | `RandomCode()` with `new Random()` | Non-deterministic reference code makes snapshot/equality tests unstable | Extracted `IReferenceGenerator` and injected it |
| `BookingService` | 79, 94 | `AuditLog.Write(...)` | Static disk write (`audit.log`); side effects and slow I/O | Injected `IAuditLog` |
| `PricingService` | 13 | `new PromoCodeRepository()` | Cannot test pricing logic in isolation from repo | Injected `IPromoCodeRepository` |
| `PricingService` | 77 | `DateTime.Today` | Promo expiry logic depends on real date | Used `_timeProvider.GetLocalNow().DateTime.Date` |
| `ReturnService` | 15-19 | `new` repo and external instances | Direct tight coupling to infrastructure | Injected interfaces via constructor |
| `ReturnService` | 37 | `DateTime.Now` | Late fees and timestamps depend on system clock | Injected `TimeProvider` |
| `ReturnService` | 64 | `AuditLog.Write(...)` | File system side-effect | Injected `IAuditLog` |
| `CancellationService` | 11-17 | `new` concrete infrastructure | Tight coupling to infrastructure classes | Injected interfaces via constructor |
| `CancellationService` | 25, 33 | `DateTime.Now` | Cancellation fee tiers depend on current time | Injected `TimeProvider` |
| `CancellationService` | 67 | `AuditLog.Write(...)` | File write | Injected `IAuditLog` |
| `PaymentGateway` | 20-22 | `Thread.Sleep(1500)` & random failure | Tests become extremely slow and flaky | Abstracted behind `IPaymentGateway` |
| `SmsSender` | 7 | `Thread.Sleep(500)` | Artificial delay slows down tests | Abstracted behind `ISmsSender` |
| `EmailSender` | 7-11 | Real file writes in `/outbox` | Tests touch actual disk and create leftover files | Abstracted behind `IEmailSender` |
| `AuditLog` | 7-8 | Static `File.AppendAllText` | I/O coupling and file lock issues across parallel tests | Turned into instance class implementing `IAuditLog` |
| `InMemoryDatabase` | 9-13 | Shared static lists | Changes in one test leak into other tests | Turned into an instance class injected into repositories |
