# Naming Conventions

- **Name**: Ammar Yasser
- **Track**: Backend .NET (Cycle 1)

---

## R1: Test Projects, Classes, and Files

1. **Test Projects**:
   - Named as `<ProjectUnderTest>.<TestType>` (e.g., `DriveNow.UnitTests`, `DriveNow.ArchitectureTests`, `ParkSmart.UnitTests`).
2. **Test Classes & Files**:
   - Every test class tests a single production class and must end with `Tests`.
   - File name matches class name: `<ClassUnderTest>Tests.cs` (e.g., `RentalMathTests.cs`, `PricingServiceTests.cs`).
   - Located in directories mirroring the production structure.

---

## R2: Test Method Naming Style

### Comparison of Known Styles:

1. **`Method_Scenario_ExpectedResult` (Roy Osherove style)**:
   - Structure: `[MethodUnderTest]_[Condition/Input]_[ExpectedOutcome]`
   - Pros: Groups tests by method in Test Explorer; immediately clear what function failed and under which condition.
   - Cons: Method name change requires test renaming.

2. **`Should_ExpectedBehavior_When_Condition`**:
   - Structure: `Should_[DoSomething]_When_[Condition]`
   - Pros: Focuses on behavior from a caller's point of view.
   - Cons: Doesn't explicitly state the exact method name, harder to sort alphabetically by method.

3. **`Given_Precondition_When_Action_Then_Result` (BDD style)**:
   - Structure: `Given_[Context]_When_[Action]_Then_[Outcome]`
   - Pros: Great for acceptance tests and non-technical stakeholders.
   - Cons: Too long and verbose for simple unit tests.

### Selected Style & Justification:
I chose **`Method_Scenario_ExpectedResult`**.
- It provides the fastest navigation when viewing failed tests in xUnit Test Explorer.
- It clearly separates what is being called, what input triggers the edge case, and what the expected result or exception is.

### 3 Examples from DriveNow:
1. `CalculateAge_BirthdayHasNotOccurredThisYear_DoesNotCountCurrentYear`
2. `CalculateRentalDays_LeftoverMinutesGreaterThanSixty_AddsExtraDay`
3. `Calculate_DriverAgeBetween21And24_AddsFifteenPercentYoungDriverSurcharge`

---

## R3: Test Doubles, Bogus, and AutoFixture Naming

1. **Hand-written Test Doubles**:
   - Prefix with the specific test double pattern:
     - `Fake<Concept>` (e.g., `FakeBookingRepository`)
     - `Stub<Concept>` (e.g., `StubPricingService`)
     - `Spy<Concept>` (e.g., `SpySmsSender`)
     - `Dummy<Concept>` (e.g., `DummyPaymentGateway`)
     - `Mock<Concept>` (e.g., `MockPaymentGateway`)
2. **Bogus Fakers**:
   - Suffix with `Faker`: `<Entity>Faker` (e.g., `CustomerFaker`, `VehicleFaker`).
3. **AutoFixture**:
   - Customizations suffix with `Customization` (e.g., `ValidCustomerCustomization`).
   - Data attributes suffix with `Attribute` (e.g., `AutoMoqDataAttribute`).

---

## R4: Production Code Naming & Namespaces

1. **Interfaces**:
   - Must start with letter `I` (e.g., `IBookingRepository`, `IPaymentGateway`).
2. **Repositories**:
   - Class name must end with `Repository` (e.g., `BookingRepository`).
   - Must live in a `.Repositories` namespace (e.g., `DriveNow.Infrastructure.Repositories`).
3. **Services**:
   - Class name must end with `Service` (e.g., `PricingService`, `BookingService`).
   - Must live in a `.Services` namespace (e.g., `DriveNow.Application.Services`).
4. **Exceptions**:
   - Class name must end with `Exception` (e.g., `RentalRuleException`).
   - Must live in `DriveNow.Domain.Exceptions`.
