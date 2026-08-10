using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Employees;
using LithoManager.Application.Features
    .HumanResources.Employees.CreateEmployee;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeById;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployees;
using LithoManager.Application.Features
    .HumanResources.Employees.SetEmployeeStatus;
using LithoManager.Application.Features
    .HumanResources.Employees.UpdateEmployee;
using LithoManager.UnitTests.TestDoubles.Persistence;
using Xunit;

namespace LithoManager.UnitTests.Features
    .HumanResources.Employees;

public sealed class EmployeeServiceTests
{
    private static readonly Guid CorrelationId =
        Guid.Parse(
            "22222222-2222-2222-2222-222222222222");

    private static readonly byte[] RowVersion =
    [
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        8
    ];

    [Fact]
    public async Task CreateAsync_WhenCommandIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        FakeEmployeeRepository repository = new();

        CreateEmployeeService service =
            new(repository);

        // Act and assert
        await Assert.ThrowsAsync<
            ArgumentNullException>(
                () => service.CreateAsync(
                    null!,
                    CancellationToken.None));

        Assert.Equal(
            0,
            repository.CreateEmployeeCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeEmployeeRepository repository = new();

        CreateEmployeeService service =
            new(repository);

        CreateEmployeeCommand command =
            CreateValidCreateCommand() with
            {
                IdentificationNumber = ""
            };

        // Act
        EmployeeResult result =
            await service.CreateAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            EmployeeErrorCode.InvalidRequest);

        Assert.Equal(
            0,
            repository.CreateEmployeeCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_CreatesEmployee()
    {
        // Arrange
        FakeEmployeeRepository repository = new();

        CreateEmployeeService service =
            new(repository);

        // Act
        EmployeeResult result =
            await service.CreateAsync(
                CreateValidCreateCommand(),
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(
            EmployeeErrorCode.None,
            result.ErrorCode);

        EmployeeInfo employee =
            Assert.IsType<EmployeeInfo>(
                result.Employee);

        Assert.Equal(20, employee.EmployeeId);
        Assert.Null(employee.UserId);
        Assert.Equal("EMP-001", employee.IdentificationNumber);

        Assert.Equal(
            1,
            repository.CreateEmployeeCallCount);
        Assert.Null(repository.LastUserId);
        Assert.Equal(10, repository.LastDepartmentId);
        Assert.Equal(
            "EMP-001",
            repository.LastIdentificationNumber);
        Assert.Equal(
            CorrelationId,
            repository.LastRequestContext?
                .CorrelationId);
    }

    [Fact]
    public async Task CreateAsync_WhenIdentificationIsDuplicated_ReturnsDuplicateIdentificationNumber()
    {
        // Arrange
        FakeEmployeeRepository repository =
            new()
            {
                ExceptionToThrow =
                    CreatePersistenceException(
                        EmployeeErrorCode
                            .DuplicateIdentificationNumber)
            };

        CreateEmployeeService service =
            new(repository);

        // Act
        EmployeeResult result =
            await service.CreateAsync(
                CreateValidCreateCommand(),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            EmployeeErrorCode
                .DuplicateIdentificationNumber);
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeEmployeeRepository repository = new();

        GetEmployeeByIdService service =
            new(repository);

        // Act
        EmployeeResult result =
            await service.GetAsync(
                employeeId: 0,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            EmployeeErrorCode.InvalidRequest);

        Assert.Equal(
            0,
            repository.GetEmployeeByIdCallCount);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEmployeeDoesNotExist_ReturnsEmployeeNotFound()
    {
        // Arrange
        FakeEmployeeRepository repository =
            new()
            {
                EmployeeByIdToReturn = null
            };

        GetEmployeeByIdService service =
            new(repository);

        // Act
        EmployeeResult result =
            await service.GetAsync(
                employeeId: 999,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            EmployeeErrorCode.EmployeeNotFound);

        Assert.Equal(
            1,
            repository.GetEmployeeByIdCallCount);
        Assert.Equal(
            999,
            repository.LastEmployeeId);
    }

    [Fact]
    public async Task GetEmployeesAsync_WhenDepartmentFilterIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeEmployeeRepository repository = new();

        GetEmployeesService service =
            new(repository);

        // Act
        EmployeesResult result =
            await service.GetAsync(
                new GetEmployeesQuery(
                    SearchTerm: null,
                    DepartmentId: 0,
                    IsActive: true),
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(
            EmployeeErrorCode.InvalidRequest,
            result.ErrorCode);
        Assert.Empty(result.Employees);

        Assert.Equal(
            0,
            repository.GetEmployeesCallCount);
    }

    [Fact]
    public async Task GetEmployeesAsync_WhenQueryIsValid_ReturnsEmployees()
    {
        // Arrange
        FakeEmployeeRepository repository = new();

        GetEmployeesService service =
            new(repository);

        // Act
        EmployeesResult result =
            await service.GetAsync(
                new GetEmployeesQuery(
                    SearchTerm: "ana",
                    DepartmentId: 10,
                    IsActive: true),
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        EmployeeInfo employee =
            Assert.Single(result.Employees);

        Assert.Equal("Ana", employee.FirstName);

        Assert.Equal(
            1,
            repository.GetEmployeesCallCount);
        Assert.Equal("ana", repository.LastSearchTerm);
        Assert.Equal(10, repository.LastDepartmentId);
        Assert.True(repository.LastIsActiveFilter);
    }

    [Fact]
    public async Task UpdateAsync_WhenRowVersionIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeEmployeeRepository repository = new();

        UpdateEmployeeService service =
            new(repository);

        UpdateEmployeeCommand command =
            CreateValidUpdateCommand() with
            {
                ExpectedRowVersion = [1, 2]
            };

        // Act
        EmployeeResult result =
            await service.UpdateAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            EmployeeErrorCode.InvalidRequest);

        Assert.Equal(
            0,
            repository.UpdateEmployeeCallCount);
    }

    [Fact]
    public async Task UpdateAsync_WhenConcurrencyConflictOccurs_ReturnsConcurrencyConflict()
    {
        // Arrange
        FakeEmployeeRepository repository =
            new()
            {
                ExceptionToThrow =
                    CreatePersistenceException(
                        EmployeeErrorCode
                            .ConcurrencyConflict)
            };

        UpdateEmployeeService service =
            new(repository);

        // Act
        EmployeeResult result =
            await service.UpdateAsync(
                CreateValidUpdateCommand(),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            EmployeeErrorCode
                .ConcurrencyConflict);
    }

    [Fact]
    public async Task UpdateAsync_WhenRequestIsValid_UpdatesEmployee()
    {
        // Arrange
        FakeEmployeeRepository repository = new();

        UpdateEmployeeService service =
            new(repository);

        // Act
        EmployeeResult result =
            await service.UpdateAsync(
                CreateValidUpdateCommand(),
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        Assert.Equal(
            1,
            repository.UpdateEmployeeCallCount);
        Assert.Equal(20, repository.LastEmployeeId);
        Assert.Equal(
            RowVersion,
            repository.LastExpectedRowVersion);
    }

    [Fact]
    public async Task SetStatusAsync_WhenDepartmentIsInactive_ReturnsDepartmentInactive()
    {
        // Arrange
        FakeEmployeeRepository repository =
            new()
            {
                ExceptionToThrow =
                    CreatePersistenceException(
                        EmployeeErrorCode
                            .DepartmentInactive)
            };

        SetEmployeeStatusService service =
            new(repository);

        // Act
        EmployeeResult result =
            await service.SetAsync(
                CreateValidSetStatusCommand(
                    isActive: true),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            EmployeeErrorCode.DepartmentInactive);
    }

    [Fact]
    public async Task SetStatusAsync_WhenRequestIsValid_SetsEmployeeStatus()
    {
        // Arrange
        FakeEmployeeRepository repository = new();

        SetEmployeeStatusService service =
            new(repository);

        // Act
        EmployeeResult result =
            await service.SetAsync(
                CreateValidSetStatusCommand(
                    isActive: false),
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        Assert.Equal(
            1,
            repository.SetEmployeeStatusCallCount);
        Assert.Equal(20, repository.LastEmployeeId);
        Assert.False(repository.LastIsActive);
        Assert.Equal(
            RowVersion,
            repository.LastExpectedRowVersion);
    }

    private static CreateEmployeeCommand
        CreateValidCreateCommand()
    {
        return new CreateEmployeeCommand(
            UserId: null,
            DepartmentId: 10,
            IdentificationNumber: "EMP-001",
            FirstName: "Ana",
            LastName: "Rivera",
            PhoneNumber: "5555-0101",
            BirthDate:
                new DateTime(
                    1990,
                    1,
                    15),
            HireDate:
                new DateTime(
                    2026,
                    8,
                    9),
            TerminationDate: null,
            JobTitle: "HR Specialist",
            BaseSalary: 1200.00m,
            ProfileImagePath: null,
            ActorUserId: 1,
            RequestContext: CreateRequestContext());
    }

    private static UpdateEmployeeCommand
        CreateValidUpdateCommand()
    {
        return new UpdateEmployeeCommand(
            EmployeeId: 20,
            UserId: null,
            DepartmentId: 10,
            IdentificationNumber: "EMP-001",
            FirstName: "Ana",
            LastName: "Rivera",
            PhoneNumber: "5555-0101",
            BirthDate:
                new DateTime(
                    1990,
                    1,
                    15),
            HireDate:
                new DateTime(
                    2026,
                    8,
                    9),
            TerminationDate: null,
            JobTitle: "HR Specialist",
            BaseSalary: 1200.00m,
            ProfileImagePath: null,
            ExpectedRowVersion:
                (byte[])RowVersion.Clone(),
            ActorUserId: 1,
            RequestContext: CreateRequestContext());
    }

    private static SetEmployeeStatusCommand
        CreateValidSetStatusCommand(
            bool isActive)
    {
        return new SetEmployeeStatusCommand(
            EmployeeId: 20,
            IsActive: isActive,
            ExpectedRowVersion:
                (byte[])RowVersion.Clone(),
            ActorUserId: 1,
            RequestContext: CreateRequestContext());
    }

    private static AuthenticationRequestContext
        CreateRequestContext()
    {
        return new AuthenticationRequestContext(
            CorrelationId:
                CorrelationId,
            ClientIpAddress:
                "127.0.0.1",
            UserAgent:
                "LithoManager.UnitTests",
            RequestPath:
                "/unit-tests/employees");
    }

    private static EmployeePersistenceException
        CreatePersistenceException(
            EmployeeErrorCode errorCode)
    {
        return new EmployeePersistenceException(
            errorCode,
            "Employee persistence failure.",
            new InvalidOperationException(
                "Persistence failure."));
    }

    private static void AssertFailure(
        EmployeeResult result,
        EmployeeErrorCode errorCode)
    {
        Assert.False(result.IsSuccessful);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Null(result.Employee);
    }
}
