# Naming Conventions

**Name:** Ammar Yasser  
**Track:** Backend .NET – Cycle 1

This document defines the naming conventions used in the DriveNow project, especially for unit tests and related test utilities.

---

## R1. Test Projects and Classes

- Test projects follow the naming format: `<ProjectName>.<TestType>`.
  - Example: `DriveNow.UnitTests`
- Each test class focuses on one production class.
- Test classes and files end with `Tests`.
  - Example: `PricingServiceTests.cs`
- Test files should follow the same folder structure as the production code whenever possible.

## R2. Test Method Naming

Test methods follow this format:

`MethodName_Scenario_ExpectedResult`

Examples:

- `CalculateAge_BirthdayNotReached_ReturnsCorrectAge`
- `CalculateRentalDays_ExtraMinutes_AddsExtraDay`
- `Calculate_YoungDriver_AddsSurcharge`

This naming style makes it easier to identify the tested method, the scenario, and the expected result when a test fails.

## R3. Test Data and Test Doubles

For manually implemented test doubles, use the following prefixes:

- `Fake`: Simplified working implementation.
- `Stub`: Returns predefined data.
- `Spy`: Records calls or interactions.
- `Dummy`: Used only to satisfy a dependency.
- `Mock`: Hand-written mock implementation, when needed.

Examples:

- `FakeBookingRepository`
- `StubPricingService`
- `SpySmsSender`
- `DummyPaymentGateway`

For Moq-based tests, use descriptive variable names such as:

- `paymentGatewayMock`
- `bookingRepositoryMock`

For Bogus data generators, use the `Faker` suffix.

Examples:

- `CustomerFaker`
- `VehicleFaker`

## R4. Production Code Naming

- Interfaces start with `I`.
  - Example: `IBookingRepository`
- Repository classes end with `Repository`.
  - Example: `BookingRepository`
- Service classes end with `Service`.
  - Example: `PricingService`
- Custom exception classes end with `Exception`.
  - Example: `RentalRuleException`

Namespaces should follow the existing project structure:

- Repositories: `DriveNow.Infrastructure.Repositories`
- Services: `DriveNow.Application.Services`
- Domain exceptions: `DriveNow.Domain.Exceptions`

---

These conventions should be followed when adding new classes and tests to the project. Existing code can be updated gradually when necessary.