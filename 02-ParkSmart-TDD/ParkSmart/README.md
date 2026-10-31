# ParkSmart

ParkSmart runs a parking garage in Alexandria and needs a fee calculator.

## Projects

- `ParkSmart.Domain` holds the parking session and the vehicle kind.
- `ParkSmart.Application` is the project for the garage workflow.
- `ParkSmart.Infrastructure` is the project for storage and outside calls.
- `ParkSmart.ConsoleApp` is the program staff will run.
- `tests/ParkSmart.UnitTests` is for behavior checks.
- `tests/ParkSmart.ArchitectureTests` is for project-reference checks.

## Build and test

```bash
dotnet build
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```
