# Test Doubles & AutoFixture Report (Task 6)
- **Name**: Ammar Yasser
- **Group / ID**: Backend .NET - Cycle 1

---

## 1. Hand-Written Test Doubles (R1)

| Double | Kind | Which Test Uses It | Why That Kind and Not Another |
|---|---|---|---|
| `DummyEmailSender` | Dummy | `CreateBooking_ValidRequest_SendsConfirmationSms` | Satisfies the `IEmailSender` dependency. The test focuses only on SMS confirmation, so email behavior is completely irrelevant. |
| `StubCustomerRepository` | Stub | `CreateBooking_ValidRequest_StoresBookingInFakeRepository` | Provides predetermined, canned `Customer` data to satisfy domain lookups without database logic. |
| `SpySmsSender` | Spy | `CreateBooking_ValidRequest_SendsConfirmationSms` | Records outgoing SMS messages in a collection (`SentMessages`) so the test can assert whether an SMS was sent, to which phone number, and containing what text. |
| `MockPaymentGateway` | Mock | `CreateBooking_StandardCustomer_ChargesDepositViaMockGateway`, `CreateBooking_PlatinumCustomer_NeverCallsPaymentGateway` | Verifies interaction expectations: asserts `Charge` was called with exact customer ID and deposit amount, or asserts it was never called for Platinum. |
| `FakeBookingRepository` | Fake | `CreateBooking_ValidRequest_StoresBookingInFakeRepository`, `CreateBooking_VehicleDatesOverlap_...` | A working in-memory implementation using a `List<Booking>`. It actually adds, updates, and queries bookings without needing a database. |

---

## 2. Hand-Written vs Moq Comparison (R2)

- **Readability**:
  - Moq tests are often more readable and concise inside the test method because `Setup`, `Returns`, and `Verify` are colocated right next to the Act and Assert sections.
  - Hand-written doubles require creating separate classes, which can clutter the project with boilerplate.
- **Maintainability & Fragility**:
  - Hand-written doubles are strongly typed; refactoring an interface gives compile-time errors in the double class immediately.
  - Moq setups can break more easily at runtime if string matching or parameter expressions (`It.IsAny<T>()`) are misconfigured, or if strict mock behavior causes unexpected invocation errors.
  - Hand-written fakes (like `FakeBookingRepository`) are easier to reuse across multiple complex scenarios than configuring 10 lines of Moq setups per test.

---

## 3. AutoFixture vs Bogus (R6)

- **When to pick Bogus**:
  - When test scenarios care about **realistic**, domain-valid data, or when specific business boundaries need to be controlled.
  - *Example in suite*: In `PricingServiceTests`, `new CustomerFaker().WithAge(21)` is used to test the exact young-driver surcharge boundary (21–24), where random dates from AutoFixture would fail the business rule.
- **When to pick AutoFixture**:
  - When tests require **anonymous** input data where the actual values do not matter at all, or when auto-wiring a system under test with its dependencies (e.g. `[Theory, AutoMoqData]`).
  - *Example in suite*: In `CreateBooking_AutoMoqFrozenGateway_ChargesExpectedDeposit`, AutoFixture automatically generates the `BookingService` constructor dependencies, reducing test setup boilerplate to zero.

---

## 4. Snapshot Stability & FIRST (R7)

- **Why an unstable snapshot is a FIRST problem**:
  - An unstable snapshot violates the **Repeatable** principle. If a snapshot captures `DateTime.Now` or a randomly generated booking code, the test will pass on the first run and fail on every subsequent run due to diff mismatches.
  - It also harms **Fast** and **Self-validating** principles because developers waste time investigating false failures rather than real regressions.
- **How stability was achieved**:
  - Injected a deterministic `FixedTimeProvider` (fixed clock).
  - Injected a `FixedReferenceGenerator` returning predictable reference codes (e.g., `"ABCD"`).
  - As a result, `CreateBooking_ValidRequest_MatchesSnapshotOutput` produces identical text output on every execution.
