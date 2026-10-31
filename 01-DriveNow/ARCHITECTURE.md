# Architecture & Naming Tests Audit
- **Name**: Ammar Yasser
- **Group / ID**: Backend .NET - Cycle 1

## Initial Violations (Task 3)

Ran 9 architecture / naming tests. 6 passed, 3 failed.

### Passed Tests

| Test | What it checks |
|------|----------------|
| `Domain_Should_NotDependOnOtherDriveNowProjects` | Domain has no reference to Application, Infrastructure, or ConsoleApp |
| `Infrastructure_Should_NotDependOnConsoleApp` | Infrastructure doesn't reference the console entry point |
| `Interfaces_Should_StartWithI` | All interfaces across Domain/Application/Infrastructure start with `I` |
| `Exceptions_Should_EndWithException_AndLiveInDomainExceptions` | Custom exceptions end with `Exception` and live in `DriveNow.Domain.Exceptions` |
| `TestClasses_Should_EndWithTests` | Every public test class name ends with `Tests` |
| `TestMethods_Should_FollowNamingConvention` | All `[Fact]`/`[Theory]` methods match `Method_Scenario_ExpectedResult` |

### Failed Tests

| Test | Failing Type | Why it fails |
|------|-------------|--------------|
| `Application_Should_DependOnlyOnDomain` | `BookingService` | It does `new CustomerRepo()`, `new PaymentGateway()`, etc. — directly depends on Infrastructure instead of using interfaces |
| `Repositories_Should_EndWithRepository_AndLiveInRepositoriesNamespace` | `CustomerRepo` | Class name ends with `Repo` not `Repository` |
| `Services_Should_EndWithService_AndLiveInServicesNamespace` | `RentalMath` | Lives in `DriveNow.Domain.Services` namespace but the class name doesn't end with `Service` |

### Root Cause

`BookingService` creates its own dependencies with `new`. There are no interfaces for
repos, gateways, or external services, so the Application layer is tightly coupled to
Infrastructure. Naming is inconsistent too (`Repo` instead of `Repository`, `RentalMath`
instead of `RentalMathService`).

These will be fixed in Task 4 (refactoring + interface extraction).

## Refactoring & Interface Placement Decisions (Task 4)

### Interface Placement Decisions

1. **Domain Interfaces (`DriveNow.Domain/Interfaces`)**:
   - `ICustomerRepository`, `IVehicleRepository`, `IBookingRepository`, `IPromoCodeRepository`
   - **Decision**: Domain entities define core business concepts. Placing repository interfaces in `Domain.Interfaces` allows both `Domain` and `Application` to depend on abstractions rather than concrete database classes (DIP). `Infrastructure` then implements these interfaces. This preserves the architecture rule that `Application` depends only on `Domain`.

2. **Application Interfaces (`DriveNow.Application/Interfaces`)**:
   - `IPaymentGateway`, `ISmsSender`, `IEmailSender`, `IAuditLog`, `IReferenceGenerator`
   - **Decision**: These interfaces represent external adapters or output ports used directly by application services to execute use cases. Placing them in `Application.Interfaces` keeps `Application` decoupled from infrastructure details (file systems, payment APIs, SMS gateways, and random generators).

### Naming & Structure Fixes

- Renamed `CustomerRepo` to `CustomerRepository` and made it implement `ICustomerRepository` in namespace `DriveNow.Infrastructure.Repositories`.
- Renamed `RentalMath` to `RentalMathService` so that all domain service classes end with `Service` and reside in `DriveNow.Domain.Services`.
- Moved `CancellationService` and `ReceiptService` into `DriveNow.Application.Services` so that every service ends with `Service` and resides in a namespace ending with `Services`.
- Converted `InMemoryDatabase` from a static class to an instance class so it can be cleanly injected into repositories and isolated between unit tests.

### Verification

After completing the refactor:
- `DriveNow.ArchitectureTests`: **9 / 9 passed** (0 failures).
- `DriveNow.UnitTests`: **14 / 14 passed**.
- `DriveNow.LegacyTests`: **5 / 5 passed**.
- `DriveNow.ConsoleApp`: builds and runs normally with manual constructor injection.
