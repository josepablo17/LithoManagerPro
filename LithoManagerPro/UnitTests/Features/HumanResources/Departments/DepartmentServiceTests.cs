using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Departments;
using LithoManager.Application.Features
    .HumanResources.Departments.CreateDepartment;
using LithoManager.Application.Features
    .HumanResources.Departments.GetDepartmentById;
using LithoManager.Application.Features
    .HumanResources.Departments.GetDepartments;
using LithoManager.Application.Features
    .HumanResources.Departments.SetDepartmentStatus;
using LithoManager.Application.Features
    .HumanResources.Departments.UpdateDepartment;
using LithoManager.UnitTests.TestDoubles.Persistence;
using Xunit;

namespace LithoManager.UnitTests.Features
    .HumanResources.Departments;

public sealed class DepartmentServiceTests
{
    private static readonly Guid CorrelationId =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

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
        FakeDepartmentRepository repository = new();

        CreateDepartmentService service =
            new(repository);

        // Act and assert
        await Assert.ThrowsAsync<
            ArgumentNullException>(
                () => service.CreateAsync(
                    null!,
                    CancellationToken.None));

        Assert.Equal(
            0,
            repository.CreateDepartmentCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeDepartmentRepository repository = new();

        CreateDepartmentService service =
            new(repository);

        CreateDepartmentCommand command =
            new(
                DepartmentCode: "",
                Name: "Human Resources",
                Description: null,
                ActorUserId: 1,
                RequestContext: CreateRequestContext());

        // Act
        DepartmentResult result =
            await service.CreateAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            DepartmentErrorCode.InvalidRequest);

        Assert.Equal(
            0,
            repository.CreateDepartmentCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_CreatesDepartment()
    {
        // Arrange
        FakeDepartmentRepository repository = new();

        CreateDepartmentService service =
            new(repository);

        CreateDepartmentCommand command =
            new(
                DepartmentCode: "HR",
                Name: "Human Resources",
                Description: "People operations.",
                ActorUserId: 1,
                RequestContext: CreateRequestContext());

        // Act
        DepartmentResult result =
            await service.CreateAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(
            DepartmentErrorCode.None,
            result.ErrorCode);

        DepartmentInfo department =
            Assert.IsType<DepartmentInfo>(
                result.Department);

        Assert.Equal(10, department.DepartmentId);
        Assert.Equal("HR", department.DepartmentCode);

        Assert.Equal(
            1,
            repository.CreateDepartmentCallCount);
        Assert.Equal("HR", repository.LastDepartmentCode);
        Assert.Equal(
            "Human Resources",
            repository.LastName);
        Assert.Equal(1, repository.LastActorUserId);
        Assert.Equal(
            CorrelationId,
            repository.LastRequestContext?
                .CorrelationId);
    }

    [Fact]
    public async Task CreateAsync_WhenCodeIsDuplicated_ReturnsDuplicateDepartmentCode()
    {
        // Arrange
        FakeDepartmentRepository repository =
            new()
            {
                ExceptionToThrow =
                    CreatePersistenceException(
                        DepartmentErrorCode
                            .DuplicateDepartmentCode)
            };

        CreateDepartmentService service =
            new(repository);

        // Act
        DepartmentResult result =
            await service.CreateAsync(
                new CreateDepartmentCommand(
                    DepartmentCode: "HR",
                    Name: "Human Resources",
                    Description: null,
                    ActorUserId: 1,
                    RequestContext:
                        CreateRequestContext()),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            DepartmentErrorCode
                .DuplicateDepartmentCode);
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeDepartmentRepository repository = new();

        GetDepartmentByIdService service =
            new(repository);

        // Act
        DepartmentResult result =
            await service.GetAsync(
                departmentId: 0,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            DepartmentErrorCode.InvalidRequest);

        Assert.Equal(
            0,
            repository.GetDepartmentByIdCallCount);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDepartmentDoesNotExist_ReturnsDepartmentNotFound()
    {
        // Arrange
        FakeDepartmentRepository repository =
            new()
            {
                DepartmentByIdToReturn = null
            };

        GetDepartmentByIdService service =
            new(repository);

        // Act
        DepartmentResult result =
            await service.GetAsync(
                departmentId: 999,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            DepartmentErrorCode.DepartmentNotFound);

        Assert.Equal(
            1,
            repository.GetDepartmentByIdCallCount);
        Assert.Equal(
            999,
            repository.LastDepartmentId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDepartmentExists_ReturnsDepartment()
    {
        // Arrange
        FakeDepartmentRepository repository = new();

        GetDepartmentByIdService service =
            new(repository);

        // Act
        DepartmentResult result =
            await service.GetAsync(
                departmentId: 10,
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Department);

        Assert.Equal(
            "Human Resources",
            result.Department.Name);
    }

    [Fact]
    public async Task GetDepartmentsAsync_WhenSearchTermIsTooLong_ReturnsInvalidRequest()
    {
        // Arrange
        FakeDepartmentRepository repository = new();

        GetDepartmentsService service =
            new(repository);

        // Act
        DepartmentsResult result =
            await service.GetAsync(
                new GetDepartmentsQuery(
                    SearchTerm:
                        new string('A', 101),
                    IsActive: true),
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(
            DepartmentErrorCode.InvalidRequest,
            result.ErrorCode);
        Assert.Empty(result.Departments);

        Assert.Equal(
            0,
            repository.GetDepartmentsCallCount);
    }

    [Fact]
    public async Task GetDepartmentsAsync_WhenQueryIsValid_ReturnsDepartments()
    {
        // Arrange
        FakeDepartmentRepository repository = new();

        GetDepartmentsService service =
            new(repository);

        // Act
        DepartmentsResult result =
            await service.GetAsync(
                new GetDepartmentsQuery(
                    SearchTerm: "hr",
                    IsActive: true),
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        DepartmentInfo department =
            Assert.Single(result.Departments);

        Assert.Equal("HR", department.DepartmentCode);

        Assert.Equal(
            1,
            repository.GetDepartmentsCallCount);
        Assert.Equal("hr", repository.LastSearchTerm);
        Assert.True(repository.LastIsActiveFilter);
    }

    [Fact]
    public async Task UpdateAsync_WhenRowVersionIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeDepartmentRepository repository = new();

        UpdateDepartmentService service =
            new(repository);

        UpdateDepartmentCommand command =
            new(
                DepartmentId: 10,
                DepartmentCode: "HR",
                Name: "Human Resources",
                Description: null,
                ExpectedRowVersion: [1, 2],
                ActorUserId: 1,
                RequestContext: CreateRequestContext());

        // Act
        DepartmentResult result =
            await service.UpdateAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            DepartmentErrorCode.InvalidRequest);

        Assert.Equal(
            0,
            repository.UpdateDepartmentCallCount);
    }

    [Fact]
    public async Task UpdateAsync_WhenConcurrencyConflictOccurs_ReturnsConcurrencyConflict()
    {
        // Arrange
        FakeDepartmentRepository repository =
            new()
            {
                ExceptionToThrow =
                    CreatePersistenceException(
                        DepartmentErrorCode
                            .ConcurrencyConflict)
            };

        UpdateDepartmentService service =
            new(repository);

        // Act
        DepartmentResult result =
            await service.UpdateAsync(
                CreateValidUpdateCommand(),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            DepartmentErrorCode
                .ConcurrencyConflict);
    }

    [Fact]
    public async Task UpdateAsync_WhenRequestIsValid_UpdatesDepartment()
    {
        // Arrange
        FakeDepartmentRepository repository = new();

        UpdateDepartmentService service =
            new(repository);

        // Act
        DepartmentResult result =
            await service.UpdateAsync(
                CreateValidUpdateCommand(),
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        Assert.Equal(
            1,
            repository.UpdateDepartmentCallCount);
        Assert.Equal(10, repository.LastDepartmentId);
        Assert.Equal(
            RowVersion,
            repository.LastExpectedRowVersion);
    }

    [Fact]
    public async Task SetStatusAsync_WhenDepartmentHasActiveEmployees_ReturnsDepartmentHasActiveEmployees()
    {
        // Arrange
        FakeDepartmentRepository repository =
            new()
            {
                ExceptionToThrow =
                    CreatePersistenceException(
                        DepartmentErrorCode
                            .DepartmentHasActiveEmployees)
            };

        SetDepartmentStatusService service =
            new(repository);

        // Act
        DepartmentResult result =
            await service.SetAsync(
                CreateValidSetStatusCommand(
                    isActive: false),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            DepartmentErrorCode
                .DepartmentHasActiveEmployees);
    }

    [Fact]
    public async Task SetStatusAsync_WhenRequestIsValid_SetsDepartmentStatus()
    {
        // Arrange
        FakeDepartmentRepository repository = new();

        SetDepartmentStatusService service =
            new(repository);

        // Act
        DepartmentResult result =
            await service.SetAsync(
                CreateValidSetStatusCommand(
                    isActive: false),
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        Assert.Equal(
            1,
            repository.SetDepartmentStatusCallCount);
        Assert.Equal(10, repository.LastDepartmentId);
        Assert.False(repository.LastIsActive);
        Assert.Equal(
            RowVersion,
            repository.LastExpectedRowVersion);
    }

    private static UpdateDepartmentCommand
        CreateValidUpdateCommand()
    {
        return new UpdateDepartmentCommand(
            DepartmentId: 10,
            DepartmentCode: "HR",
            Name: "Human Resources",
            Description: "People operations.",
            ExpectedRowVersion:
                (byte[])RowVersion.Clone(),
            ActorUserId: 1,
            RequestContext: CreateRequestContext());
    }

    private static SetDepartmentStatusCommand
        CreateValidSetStatusCommand(
            bool isActive)
    {
        return new SetDepartmentStatusCommand(
            DepartmentId: 10,
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
                "/unit-tests/departments");
    }

    private static DepartmentPersistenceException
        CreatePersistenceException(
            DepartmentErrorCode errorCode)
    {
        return new DepartmentPersistenceException(
            errorCode,
            "Department persistence failure.",
            new InvalidOperationException(
                "Persistence failure."));
    }

    private static void AssertFailure(
        DepartmentResult result,
        DepartmentErrorCode errorCode)
    {
        Assert.False(result.IsSuccessful);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Null(result.Department);
    }
}
