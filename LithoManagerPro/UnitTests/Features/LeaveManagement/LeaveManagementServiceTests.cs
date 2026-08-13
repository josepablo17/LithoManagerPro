using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.LeaveManagement;
using LithoManager.Application.Features.LeaveManagement
    .AdjustEmployeeLeaveBalance;
using LithoManager.Application.Features.LeaveManagement
    .CancelLeaveRequest;
using LithoManager.Application.Features.LeaveManagement
    .CreateLeaveRequest;
using LithoManager.Application.Features.LeaveManagement
    .GetEmployeeLeaveBalance;
using LithoManager.Application.Features.LeaveManagement
    .GetLeaveRequests;
using LithoManager.Application.Features.LeaveManagement
    .RespondLeaveRequest;
using LithoManager.UnitTests.TestDoubles.Persistence;
using Xunit;

namespace LithoManager.UnitTests.Features.LeaveManagement;

public sealed class LeaveManagementServiceTests
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
    public async Task CreateAsync_WhenEndDateIsBeforeStartDate_ReturnsInvalidRequest()
    {
        // Arrange
        FakeLeaveManagementRepository repository =
            new();

        CreateLeaveRequestService service =
            new(repository);

        CreateLeaveRequestCommand command =
            new(
                StartDate: new DateTime(2026, 9, 16),
                EndDate: new DateTime(2026, 9, 14),
                ActorUserId: 1,
                LeaveTypeCode: "Vacation",
                RequestContext: CreateRequestContext());

        // Act
        LeaveRequestResult result =
            await service.CreateAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            LeaveManagementErrorCode.InvalidRequest);

        Assert.Equal(
            0,
            repository.CreateLeaveRequestCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenLeaveTypeIsOmitted_UsesVacation()
    {
        // Arrange
        FakeLeaveManagementRepository repository =
            new();

        CreateLeaveRequestService service =
            new(repository);

        CreateLeaveRequestCommand command =
            new(
                StartDate: new DateTime(2026, 9, 14),
                EndDate: new DateTime(2026, 9, 16),
                ActorUserId: 1,
                LeaveTypeCode: null,
                RequestContext: CreateRequestContext());

        // Act
        LeaveRequestResult result =
            await service.CreateAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.LeaveRequest);
        Assert.Equal(
            "Vacation",
            repository.LastLeaveTypeCode);
        Assert.Equal(
            1,
            repository.CreateLeaveRequestCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenBalanceIsInsufficient_ReturnsInsufficientLeaveBalance()
    {
        // Arrange
        FakeLeaveManagementRepository repository =
            new()
            {
                ExceptionToThrow =
                    CreatePersistenceException(
                        LeaveManagementErrorCode
                            .InsufficientLeaveBalance)
            };

        CreateLeaveRequestService service =
            new(repository);

        // Act
        LeaveRequestResult result =
            await service.CreateAsync(
                CreateValidCreateCommand(),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            LeaveManagementErrorCode
                .InsufficientLeaveBalance);
    }

    [Fact]
    public async Task GetLeaveRequestsAsync_WhenStatusIsOmitted_DoesNotFilterByStatus()
    {
        // Arrange
        FakeLeaveManagementRepository repository =
            new();

        GetLeaveRequestsService service =
            new(repository);

        GetLeaveRequestsQuery query =
            new(
                ActorUserId: 1,
                LeaveRequestStatusCode: null,
                EmployeeId: null,
                DepartmentId: null,
                StartDateFrom: null,
                StartDateTo: null,
                SearchTerm: null);

        // Act
        LeaveRequestsResult result =
            await service.GetAsync(
                query,
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(
            repository.LastLeaveRequestStatusCode);
        Assert.Equal(
            1,
            repository.GetLeaveRequestsCallCount);
    }

    [Fact]
    public async Task AdjustAsync_WhenDeltaIsZero_ReturnsInvalidRequest()
    {
        // Arrange
        FakeLeaveManagementRepository repository =
            new();

        AdjustEmployeeLeaveBalanceService service =
            new(repository);

        AdjustEmployeeLeaveBalanceCommand command =
            new(
                EmployeeId: 30,
                LeaveTypeCode: "Vacation",
                AdjustedDaysDelta: 0,
                ActorUserId: 1,
                RequestContext: CreateRequestContext());

        // Act
        EmployeeLeaveBalanceResult result =
            await service.AdjustAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            LeaveManagementErrorCode.InvalidRequest);

        Assert.Equal(
            0,
            repository.AdjustEmployeeLeaveBalanceCallCount);
    }

    [Fact]
    public async Task GetEmployeeLeaveBalanceAsync_WhenBalanceDoesNotExist_ReturnsLeaveBalanceNotFound()
    {
        // Arrange
        FakeLeaveManagementRepository repository =
            new()
            {
                EmployeeLeaveBalanceToReturn = null
            };

        GetEmployeeLeaveBalanceService service =
            new(repository);

        GetEmployeeLeaveBalanceQuery query =
            new(
                EmployeeId: 30,
                LeaveTypeCode: "Vacation",
                ActorUserId: 1);

        // Act
        EmployeeLeaveBalanceResult result =
            await service.GetAsync(
                query,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            LeaveManagementErrorCode.LeaveBalanceNotFound);

        Assert.Equal(
            1,
            repository.GetEmployeeLeaveBalanceCallCount);
    }

    [Fact]
    public async Task CancelAsync_WhenRowVersionIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeLeaveManagementRepository repository =
            new();

        CancelLeaveRequestService service =
            new(repository);

        CancelLeaveRequestCommand command =
            new(
                LeaveRequestId: 20,
                ExpectedRowVersion: [1, 2, 3],
                ActorUserId: 1,
                RequestContext: CreateRequestContext());

        // Act
        LeaveRequestResult result =
            await service.CancelAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            LeaveManagementErrorCode.InvalidRequest);

        Assert.Equal(
            0,
            repository.CancelLeaveRequestCallCount);
    }

    [Fact]
    public async Task RespondAsync_WhenRowVersionIsStale_ReturnsConcurrencyConflict()
    {
        // Arrange
        FakeLeaveManagementRepository repository =
            new()
            {
                ExceptionToThrow =
                    CreatePersistenceException(
                        LeaveManagementErrorCode
                            .ConcurrencyConflict)
            };

        RespondLeaveRequestService service =
            new(repository);

        RespondLeaveRequestCommand command =
            new(
                LeaveRequestId: 20,
                IsApproved: true,
                ExpectedRowVersion: RowVersion,
                ActorUserId: 1,
                RequestContext: CreateRequestContext());

        // Act
        LeaveRequestResult result =
            await service.RespondAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            LeaveManagementErrorCode.ConcurrencyConflict);

        Assert.Equal(
            1,
            repository.RespondLeaveRequestCallCount);
    }

    private static CreateLeaveRequestCommand
        CreateValidCreateCommand()
    {
        return new CreateLeaveRequestCommand(
            StartDate: new DateTime(2026, 9, 14),
            EndDate: new DateTime(2026, 9, 16),
            ActorUserId: 1,
            LeaveTypeCode: "Vacation",
            RequestContext: CreateRequestContext());
    }

    private static AuthenticationRequestContext
        CreateRequestContext()
    {
        return new AuthenticationRequestContext(
            CorrelationId,
            ClientIpAddress: "127.0.0.1",
            UserAgent: "LithoManager.UnitTests",
            RequestPath:
                "/unit-tests/leave-management");
    }

    private static LeaveManagementPersistenceException
        CreatePersistenceException(
            LeaveManagementErrorCode errorCode)
    {
        return new LeaveManagementPersistenceException(
            errorCode,
            "Persistence error.",
            new InvalidOperationException(
                "Test persistence exception."));
    }

    private static void AssertFailure(
        LeaveRequestResult result,
        LeaveManagementErrorCode errorCode)
    {
        Assert.False(result.IsSuccessful);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Null(result.LeaveRequest);
    }

    private static void AssertFailure(
        LeaveRequestsResult result,
        LeaveManagementErrorCode errorCode)
    {
        Assert.False(result.IsSuccessful);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Empty(result.LeaveRequests);
    }

    private static void AssertFailure(
        EmployeeLeaveBalanceResult result,
        LeaveManagementErrorCode errorCode)
    {
        Assert.False(result.IsSuccessful);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Null(result.LeaveBalance);
    }
}
