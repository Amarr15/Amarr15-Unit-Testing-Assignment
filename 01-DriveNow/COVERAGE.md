# Code Coverage Progress & Analysis (Task 9)
- **Name**: Ammar Yasser
- **Group / ID**: Backend .NET - Cycle 1

## Coverage Progress Table

| After Task | Line % (Overall) | Branch % (Overall) | Application Line % | Application Branch % | Domain Line % | Domain Branch % | Notes |
|---|---|---|---|---|---|---|---|
| Baseline (Legacy only) | 4.79% | 2.67% | 7.80% | 3.22% | 3.96% | 0.00% | Only 5 legacy tests in `DriveNow.LegacyTests` |
| Task 8 (Final) | 22.6% | 59.7% | 96.8% | 91.8% | 94.7% | 83.3% | 124 unit + 2 legacy + 9 arch = 135 tests |

> **Note**: The overall percentages are low because they include `DriveNow.Infrastructure` (0%, intentionally untested) and `DriveNow.UnitTests` (0%, test code shouldn't count). The meaningful metrics are Application and Domain columns.

## R3 - Exclusions Rationale

1. **`DriveNow.ConsoleApp` (`Program.cs`)**: Excluded via `[ExcludeFromCodeCoverage]`. This is the console UI entry point containing `Main()`, menu loops, user input parsing (`ReadInt`, `ReadDate`, `ReadDecimal`), and `Console.WriteLine` display logic. None of this is business logic; it's glue code that wires up dependencies and interacts with `stdin`/`stdout`. Testing it would require simulating console I/O, which is integration-level work and not meaningful for unit test coverage.

2. **`DriveNow.Infrastructure`** (0% coverage): All 11 infrastructure classes (`InMemoryDatabase`, repositories, `PaymentGateway`, `SmsSender`, `EmailSender`, `AuditLog`, `DefaultReferenceGenerator`) are intentionally untested because we replaced them with test doubles (stubs, fakes, spies, mocks) in our unit tests. This is the correct approach: unit tests isolate the SUT from infrastructure concerns. Testing these concrete implementations belongs in integration tests.

3. **`DriveNow.UnitTests`** (0% coverage): Test project code is never a coverage target. Coverlet instruments it by default but it's meaningless self-coverage.

## R5 - Coverage is Not Correctness Example

**100% line coverage does NOT mean the code is correct.** Here are concrete examples from our own codebase:

| Class | Line Coverage | Branch Coverage | Known Bug |
|---|---|---|---|
| `RentalMathService` | 100% | 100% | **BR-03**: Uses `>= 60` instead of `> 60` for the grace-hour threshold, giving an extra day for exactly 60 leftover minutes. |
| `PricingService` | 100% | 100% | **BR-33**: Uses `days > 7 && days < 14` instead of `days >= 7`, giving 0% duration discount for exactly 7 days. |
| `PricingService` | 100% | 100% | **BR-37**: Discount cap only reduces duration+loyalty but never reduces promo, violating the business rule. |
| `PricingService` | 100% | 100% | **BR-39**: Gives free additional driver to Silver tier (`>= Silver`) instead of only Gold/Platinum. |

Every line in these services is executed during our tests, yet 4 bugs survive. The tests that exercise these lines use inputs that happen to avoid the buggy boundary values or accept the buggy output because we wrote them to match the current (buggy) implementation. Coverage tells us "this code ran" but says nothing about "this code produced the right answer."

## R6 - Intentional Uncovered Branch Gap

**`LoyaltyService.UpgradeTier`** — Line coverage 70%, Branch coverage 50% (3 of 6 branches covered).

The uncovered branches are:
- The `completedRentals >= 30` path (Platinum upgrade)
- The `completedRentals >= 15` path (Gold upgrade)

These branches are only reachable through `ReturnService.ProcessReturn`, which increments `CompletedRentals` by 1 per call. Our test customers start with 0 completed rentals. To hit the Platinum branch, we would need a customer pre-seeded with 29 completed rentals, process a return, and assert the tier changed to Platinum. While this test is simple to write, it's a pure threshold comparison with no complex logic — the risk of a bug is extremely low and the test setup cost outweighs the value. The Silver branch (>= 5) is already covered indirectly, confirming the pattern works. Covering the remaining thresholds would be a mechanical exercise that adds no meaningful confidence.
