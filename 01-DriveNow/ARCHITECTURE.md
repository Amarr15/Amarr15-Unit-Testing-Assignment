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

_(to be filled after Task 4)_
