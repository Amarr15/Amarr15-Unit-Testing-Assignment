# FIRST Audit (Task 2)
- **Name**: Ammar Yasser
- **Group / ID**: Backend .NET - Cycle 1

---

## 1. Test Run Records (5 Suite Runs)

The full test suite was executed 5 consecutive times without modifying production code.

| Run | Total Suite Duration | Test Result | Failed Tests | Failure Reason |
|---|---|---|---|---|
| **Run 1** | 7.30 s | Failed (16 passed, 1 failed) | `CreateBooking_OverlappingDates_ThrowsBookingRejectedException` | `BookingRejectedException` thrown during setup call because vehicle was already reserved by the preceding test. |
| **Run 2** | 7.01 s | Failed (16 passed, 1 failed) | `CreateBooking_OverlappingDates_ThrowsBookingRejectedException` | Same shared static database interference. |
| **Run 3** | 7.10 s | Failed (16 passed, 1 failed) | `CreateBooking_OverlappingDates_ThrowsBookingRejectedException` | Same shared static database interference. |
| **Run 4** | 8.50 s | Failed (16 passed, 1 failed) | `CreateBooking_ValidRequest_CreatesBookingSuccessfully` | `PaymentFailedException: Gateway timeout` (random 10% simulated failure). |
| **Run 5** | 8.78 s | Failed (16 passed, 1 failed) | `CreateBooking_ValidRequest_CreatesBookingSuccessfully` | `PaymentFailedException: Gateway timeout` (random failure repeated). |

---

## 2. FIRST Principles Evaluation

### F — Fast: **BROKEN**
- **Evidence**: Running only 3 naive booking tests pushed total execution time to **over 7–8 seconds** (more than 2.0s per booking test).
- **Impact**: In a large production test suite of thousands of tests, this would make running unit tests take minutes or hours, destroying developer feedback loops.

### I — Independent: **BROKEN**
- **Evidence**: `CreateBooking_OverlappingDates_ThrowsBookingRejectedException` **passes completely when run in isolation** (`Passed in 2.0s`), but **fails when run as part of the full suite**.
- **Reason**: The test before it (`CreateBooking_ValidRequest_CreatesBookingSuccessfully`) created a booking for Vehicle 1 that was never cleared from the shared in-memory database.

### R — Repeatable: **BROKEN**
- **Evidence**: `CreateBooking_ValidRequest_CreatesBookingSuccessfully` passed in Runs 1–3, but failed in Runs 4 and 5 with `PaymentFailedException: Gateway timeout`.
- **Reason**: A randomized timeout simulation causes flaky test runs that cannot be relied upon in CI/CD pipelines.

### S — Self-validating: **BROKEN**
- **Evidence**: Tests produce persistent side-effects outside of the test memory space, including console output (`Console.WriteLine`) and disk file writes (`audit.log` file created in output folder).

### T — Timely: **BROKEN**
- **Evidence**: `BookingService` directly accesses the system wall clock (`DateTime.Now`). Tests cannot simulate different times of day, past bookings, or fixed future dates without coupling to the machine clock at execution time.

---

## 3. Offending Production Code Lines

| FIRST Violation | File Path | Line # | Offending Code Snippet | Root Problem |
|---|---|---|---|---|
| **Fast** | `DriveNow.Infrastructure/External/PaymentGateway.cs` | Line 27 | `Thread.Sleep(1500);` | Artificial sleep blocking the test thread. |
| **Fast** | `DriveNow.Infrastructure/External/SmsSender.cs` | Line 7 | `Thread.Sleep(500);` | Artificial sleep on external notification. |
| **Independent** | `DriveNow.Infrastructure/Persistence/InMemoryDatabase.cs` | Line 9 | `public static List<Booking> Bookings = [];` | Static shared in-memory state shared across tests. |
| **Independent** | `DriveNow.Application/Services/BookingService.cs` | Line 64 | `_bookings.Add(booking);` | Directly writes to the shared static store without cleanup or reset. |
| **Repeatable** | `DriveNow.Infrastructure/External/PaymentGateway.cs` | Line 28 | `if (new Random().Next(10) == 0) throw new PaymentFailedException("Gateway timeout");` | Non-deterministic random failure (10% flake rate). |
| **Repeatable** | `DriveNow.Application/Services/BookingService.cs` | Line 48 | `Reference = "DN-" + ... + RandomCode()` | Random booking reference generation prevents stable assertions. |
| **Self-validating** | `DriveNow.Infrastructure/Logging/AuditLog.cs` | Line 9 | `File.AppendAllText(path, $"[{DateTime.Now:O}] {message}...");` | Disk I/O side effect creating real `audit.log` files during test execution. |
| **Timely** | `DriveNow.Application/Services/BookingService.cs` | Line 46, 104 | `CreatedAt = DateTime.Now`, `if (request.PickupAt < DateTime.Now.AddHours(2))` | Hardcoded dependency on machine clock rather than an injectable `TimeProvider`. |
