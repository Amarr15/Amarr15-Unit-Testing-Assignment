using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace DriveNow.ArchitectureTests;

public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(DriveNow.Domain.Entities.Booking).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(DriveNow.Application.Services.BookingService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(DriveNow.Infrastructure.Repositories.BookingRepository).Assembly;
    private static readonly Assembly UnitTestsAssembly = typeof(DriveNow.UnitTests.Domain.Services.RentalMathTests).Assembly;

    // R1: Domain depends on no other DriveNow project
    [Fact]
    public void Domain_Should_NotDependOnOtherDriveNowProjects()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("DriveNow.Application", "DriveNow.Infrastructure", "DriveNow.ConsoleApp")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    // R2: Application depends only on Domain (not Infrastructure, not ConsoleApp)
    [Fact]
    public void Application_Should_DependOnlyOnDomain()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("DriveNow.Infrastructure", "DriveNow.ConsoleApp")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    // R3: Infrastructure does not depend on ConsoleApp
    [Fact]
    public void Infrastructure_Should_NotDependOnConsoleApp()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("DriveNow.ConsoleApp")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    // R4: Interfaces start with 'I'
    [Fact]
    public void Interfaces_Should_StartWithI()
    {
        var result = Types.InAssemblies([DomainAssembly, ApplicationAssembly, InfrastructureAssembly])
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    // R4: Repositories end with 'Repository' and live in Repositories namespace
    [Fact]
    public void Repositories_Should_EndWithRepository_AndLiveInRepositoriesNamespace()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespaceEndingWith("Repositories")
            .Or().HaveNameEndingWith("Repository")
            .Or().HaveNameEndingWith("Repo")
            .Should()
            .HaveNameEndingWith("Repository")
            .And()
            .ResideInNamespaceEndingWith("Repositories")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    // R4: Services end with 'Service' and live in Services namespace
    [Fact]
    public void Services_Should_EndWithService_AndLiveInServicesNamespace()
    {
        var result = Types.InAssemblies([DomainAssembly, ApplicationAssembly])
            .That()
            .ResideInNamespaceEndingWith("Services")
            .Or().HaveNameEndingWith("Service")
            .Should()
            .HaveNameEndingWith("Service")
            .And()
            .ResideInNamespaceEndingWith("Services")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    // R4: Exceptions end with 'Exception' and live in DriveNow.Domain.Exceptions
    [Fact]
    public void Exceptions_Should_EndWithException_AndLiveInDomainExceptions()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(Exception))
            .Should()
            .HaveNameEndingWith("Exception")
            .And()
            .ResideInNamespace("DriveNow.Domain.Exceptions")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    // R5: Test classes end with 'Tests'
    [Fact]
    public void TestClasses_Should_EndWithTests()
    {
        var testClasses = UnitTestsAssembly.GetTypes()
            .Where(t => t.IsClass && t.IsPublic &&
                        t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .Any(m => m.GetCustomAttributes(typeof(FactAttribute), true).Any() ||
                                   m.GetCustomAttributes(typeof(TheoryAttribute), true).Any()))
            .ToList();

        testClasses.Should().OnlyContain(t => t.Name.EndsWith("Tests"));
    }

    // R5: Test methods match Method_Scenario_ExpectedResult
    [Fact]
    public void TestMethods_Should_FollowNamingConvention()
    {
        var testMethods = UnitTestsAssembly.GetTypes()
            .Where(t => t.IsPublic && t.Name.EndsWith("Tests"))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttributes(typeof(FactAttribute), true).Any() ||
                        m.GetCustomAttributes(typeof(TheoryAttribute), true).Any())
            .ToList();

        var pattern = new Regex(@"^[A-Za-z0-9]+_[A-Za-z0-9]+_[A-Za-z0-9]+$");

        var invalidNames = testMethods
            .Where(m => !pattern.IsMatch(m.Name))
            .Select(m => $"{m.DeclaringType?.Name}.{m.Name}")
            .ToList();

        invalidNames.Should().BeEmpty("all unit test methods must follow Method_Scenario_ExpectedResult");
    }
}
