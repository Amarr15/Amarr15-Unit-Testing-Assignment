# LinkedIn Posts - Assignment 9

Here are the 4 ready-to-publish posts for Assignment 9, each over 150 words, complete with code snippets, real project examples, and hashtags.

---

## Post Links
1. **Test Doubles**: https://www.linkedin.com/posts/...
2. **Bogus & Deterministic Data**: https://www.linkedin.com/posts/...
3. **TDD in Practice**: https://www.linkedin.com/posts/...
4. **Code Coverage vs Reality**: https://www.linkedin.com/posts/...

---

## Post 1: Test Doubles Explained with Real .NET Examples

Many developers use the word "Mock" for every test helper, but in reality, Gerard Meszaros categorized Test Doubles into five distinct types. In my recent work building unit tests for **DriveNow**, I implemented and used all five:

1. **Dummy**: An object passed solely to fill parameter lists and satisfy non-null constructor dependencies, never actually accessed during test execution (e.g., `DummySmsSender` when testing pricing logic).
2. **Stub**: An object providing canned, predefined answers to calls made during the test without logic (e.g., `StubCustomerRepository` returning a customer with a Gold loyalty tier).
3. **Spy**: A wrapper that records what happened (calls received, arguments captured) so we can inspect and assert them later (e.g., `SpyAuditLog` verifying how many times an audit event was emitted).
4. **Mock**: An object pre-programmed with explicit behavioral expectations that will fail the test if calls don't match (e.g., verifying `_paymentGatewayMock.Verify(x => x.Charge(500m), Times.Once)`).
5. **Fake**: A lightweight, working implementation with simplified business logic unsuitable for production (e.g., `FakeBookingRepository` using an in-memory `ConcurrentDictionary<Guid, Booking>`).

Understanding the distinction helps you keep tests lightweight, fast, and resilient against implementation changes.

```csharp
// Example: Hand-crafted Spy in C#
public class SpyAuditLog : IAuditLog
{
    public List<string> LoggedEvents { get; } = new();
    public void Log(string message) => LoggedEvents.Add(message);
}
```

#SimulationAcademy #DotNet #CSharp #UnitTesting

---

## Post 2: Smarter Test Data with Bogus and Deterministic Seeds

Hardcoded values like `"John Doe"` or `"test@test.com"` often make unit tests brittle and hide real-world edge cases. That's where **Bogus** comes in handy for .NET developers.

With Bogus, you can create clean, realistic fluent domain fakers:

```csharp
var customerFaker = new Faker<Customer>()
    .RuleFor(c => c.Id, f => Guid.NewGuid())
    .RuleFor(c => c.FullName, f => f.Name.FullName())
    .RuleFor(c => c.Email, f => f.Internet.Email())
    .RuleFor(c => c.Tier, f => f.PickRandom<CustomerTier>());
```

However, there is a golden rule: **Never let test data be purely random without a fixed seed.** Purely non-deterministic data leads to "flaky tests" that pass 99 times and fail on the 100th build in CI because of an unhandled unicode character or leap-year timestamp.

To guarantee 100% repeatability while retaining diverse realistic test data, always set a deterministic global seed:
```csharp
Randomizer.Seed = new Random(1337);
```

This gives you the best of both worlds: diverse, realistic production-like objects with 100% deterministic, reproducible test runs every single time!

#SimulationAcademy #DotNet #CSharp #UnitTesting

---

## Post 3: Lessons Learned Building ParkSmart with Pure TDD

Test-Driven Development (TDD) sounds simple on paper: **Red -> Green -> Refactor**. But putting it into strict practice across 10 distinct feature stages in **ParkSmart** taught me deep architectural lessons.

The hardest challenge occurred when moving from basic hourly fees (Stage S2) to the **24-hour daily cap** (Stage S4) and the **night flat rate window** (Stage S6). 

In the beginning, calculating the fee seemed like a straightforward formula: `hours * rate`. But as soon as Stage S4 required a 80 EGP daily cap for each 24-hour block, and Stage S6 introduced night windows (22:00 to 08:00 next day) with lower flat rates, the naive multiplication broke completely. 

Because I had an automated suite of fast, reliable unit tests catching every regression, I was able to safely refactor the core engine:
- Decomposed parking duration into full 24-hour blocks plus remaining hours.
- Evaluated time-of-day boundaries independently.
- Layered subscriber discounts and EV charging cleanly in a sequential pipeline.

TDD forces you to design from the consumer's perspective first, keeping code modular, readable, and fearless when requirements shift.

#SimulationAcademy #DotNet #CSharp #UnitTesting

---

## Post 4: Why 100% Code Coverage Doesn't Mean Zero Bugs

In our industry, high test coverage is often mistaken for high software quality. But after working through comprehensive unit test audits on **DriveNow**, the difference between **Line Coverage** and **Branch Coverage** became crystal clear.

You can easily achieve 90%+ line coverage by simply executing the "happy path" of every method. However, critical software bugs almost always hide at the edges of conditional branches:

1. **Boundary Off-by-One Errors**: In BR-03, rental durations allow a 60-minute grace period. A naive `<` instead of `<=` charged an entire extra day for returning a vehicle at exactly 60 minutes late.
2. **Cap Headroom Ordering**: In BR-37, total discounts were capped at 25%. A naive implementation capped the loyalty tier instead of decrementing the promo code discount first.
3. **Discount Exclusions**: EV charging and lost ticket minimum fees must bypass standard discounts.

Tools like **Coverlet** and OpenCover help measure line and branch coverage, but true confidence comes from disciplined **Equivalence Partitioning** and **Boundary Value Analysis (BVA)**. Don't just aim for green coverage reports—test the boundaries that matter!

#SimulationAcademy #DotNet #CSharp #UnitTesting
