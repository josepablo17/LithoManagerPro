using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.LeaveManagement;

namespace LithoManager.UnitTests.TestDoubles.Persistence;

public sealed class FakeLeaveManagementRepository
    : ILeaveManagementRepository
{
    public LeaveRequestData LeaveRequestToReturn
    {
        get;
        set;
    } = CreateDefaultLeaveRequest();

    public IReadOnlyList<LeaveRequestData>
        LeaveRequestsToReturn
    {
        get;
        set;
    } = [CreateDefaultLeaveRequest()];

    public EmployeeLeaveBalanceData?
        EmployeeLeaveBalanceToReturn
    {
        get;
        set;
    } = CreateDefaultLeaveBalance();

    public IReadOnlyList<LeaveTypeData>
        LeaveTypesToReturn
    {
        get;
        set;
    } =
    [
        new LeaveTypeData
        {
            LeaveTypeId = 1,
            LeaveTypeCode = "Vacation",
            Name = "Vacation",
            AffectsVacationBalance = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            RowVersion =
            [
                1, 2, 3, 4, 5, 6, 7, 8
            ]
        }
    ];

    public IReadOnlyList<LeaveRequestStatusData>
        LeaveRequestStatusesToReturn
    {
        get;
        set;
    } =
    [
        new LeaveRequestStatusData
        {
            LeaveRequestStatusCode = "Pending",
            Name = "Pending",
            SortOrder = 1,
            IsTerminal = false,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            RowVersion =
            [
                1, 2, 3, 4, 5, 6, 7, 8
            ]
        }
    ];

    public LeaveManagementPersistenceException?
        ExceptionToThrow
    {
        get;
        set;
    }

    public int CreateLeaveRequestCallCount
    {
        get;
        private set;
    }

    public int GetLeaveRequestsCallCount
    {
        get;
        private set;
    }

    public int AdjustEmployeeLeaveBalanceCallCount
    {
        get;
        private set;
    }

    public int GetEmployeeLeaveBalanceCallCount
    {
        get;
        private set;
    }

    public int CancelLeaveRequestCallCount
    {
        get;
        private set;
    }

    public int RespondLeaveRequestCallCount
    {
        get;
        private set;
    }

    public string? LastLeaveTypeCode
    {
        get;
        private set;
    }

    public string? LastLeaveRequestStatusCode
    {
        get;
        private set;
    }

    public DateTime? LastStartDate
    {
        get;
        private set;
    }

    public DateTime? LastEndDate
    {
        get;
        private set;
    }

    public int? LastActorUserId
    {
        get;
        private set;
    }

    public int? LastEmployeeId
    {
        get;
        private set;
    }

    public decimal? LastAdjustedDaysDelta
    {
        get;
        private set;
    }

    public byte[]? LastExpectedRowVersion
    {
        get;
        private set;
    }

    public bool? LastIsApproved
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        LastRequestContext
    {
        get;
        private set;
    }

    public Task<IReadOnlyList<LeaveTypeData>>
        GetLeaveTypesAsync(
            bool? isActive,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(LeaveTypesToReturn);
    }

    public Task<IReadOnlyList<LeaveRequestStatusData>>
        GetLeaveRequestStatusesAsync(
            bool? isActive,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            LeaveRequestStatusesToReturn);
    }

    public Task<EmployeeLeaveBalanceData?>
        GetEmployeeLeaveBalanceAsync(
            int? employeeId,
            string leaveTypeCode,
            int actorUserId,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetEmployeeLeaveBalanceCallCount++;
        LastEmployeeId = employeeId;
        LastLeaveTypeCode = leaveTypeCode;
        LastActorUserId = actorUserId;

        ThrowIfConfigured();

        return Task.FromResult(
            EmployeeLeaveBalanceToReturn);
    }

    public Task<EmployeeLeaveBalanceData>
        AdjustEmployeeLeaveBalanceAsync(
            int employeeId,
            string leaveTypeCode,
            decimal adjustedDaysDelta,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        AdjustEmployeeLeaveBalanceCallCount++;
        LastEmployeeId = employeeId;
        LastLeaveTypeCode = leaveTypeCode;
        LastAdjustedDaysDelta = adjustedDaysDelta;
        LastActorUserId = actorUserId;
        LastRequestContext = requestContext;

        ThrowIfConfigured();

        return Task.FromResult(
            EmployeeLeaveBalanceToReturn
            ?? CreateDefaultLeaveBalance());
    }

    public Task<IReadOnlyList<LeaveRequestData>>
        GetMyLeaveRequestsAsync(
            int actorUserId,
            string? leaveRequestStatusCode,
            DateTime? startDateFrom,
            DateTime? startDateTo,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        LastActorUserId = actorUserId;
        LastLeaveRequestStatusCode =
            leaveRequestStatusCode;

        ThrowIfConfigured();

        return Task.FromResult(
            LeaveRequestsToReturn);
    }

    public Task<IReadOnlyList<LeaveRequestData>>
        GetLeaveRequestsAsync(
            int actorUserId,
            string? leaveRequestStatusCode,
            int? employeeId,
            int? departmentId,
            DateTime? startDateFrom,
            DateTime? startDateTo,
            string? searchTerm,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetLeaveRequestsCallCount++;
        LastActorUserId = actorUserId;
        LastLeaveRequestStatusCode =
            leaveRequestStatusCode;
        LastEmployeeId = employeeId;

        ThrowIfConfigured();

        return Task.FromResult(
            LeaveRequestsToReturn);
    }

    public Task<LeaveRequestData?> GetLeaveRequestByIdAsync(
        int leaveRequestId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        LastActorUserId = actorUserId;

        ThrowIfConfigured();

        return Task.FromResult<LeaveRequestData?>(
            LeaveRequestToReturn);
    }

    public Task<LeaveRequestData> CreateLeaveRequestAsync(
        DateTime startDate,
        DateTime endDate,
        int actorUserId,
        string leaveTypeCode,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CreateLeaveRequestCallCount++;
        LastStartDate = startDate;
        LastEndDate = endDate;
        LastActorUserId = actorUserId;
        LastLeaveTypeCode = leaveTypeCode;
        LastRequestContext = requestContext;

        ThrowIfConfigured();

        return Task.FromResult(LeaveRequestToReturn);
    }

    public Task<LeaveRequestData> CancelLeaveRequestAsync(
        int leaveRequestId,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CancelLeaveRequestCallCount++;
        LastExpectedRowVersion =
            (byte[])expectedRowVersion.Clone();
        LastActorUserId = actorUserId;
        LastRequestContext = requestContext;

        ThrowIfConfigured();

        return Task.FromResult(LeaveRequestToReturn);
    }

    public Task<LeaveRequestData> RespondLeaveRequestAsync(
        int leaveRequestId,
        bool isApproved,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RespondLeaveRequestCallCount++;
        LastIsApproved = isApproved;
        LastExpectedRowVersion =
            (byte[])expectedRowVersion.Clone();
        LastActorUserId = actorUserId;
        LastRequestContext = requestContext;

        ThrowIfConfigured();

        return Task.FromResult(LeaveRequestToReturn);
    }

    private void ThrowIfConfigured()
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }
    }

    private static LeaveRequestData
        CreateDefaultLeaveRequest()
    {
        return new LeaveRequestData
        {
            LeaveRequestId = 20,
            EmployeeId = 30,
            IdentificationNumber = "EMP-001",
            FirstName = "Integration",
            LastName = "User",
            DepartmentId = 40,
            DepartmentCode = "HR",
            DepartmentName = "Human Resources",
            LeaveTypeId = 1,
            LeaveTypeCode = "Vacation",
            LeaveTypeName = "Vacation",
            LeaveRequestStatusCode = "Pending",
            LeaveRequestStatusName = "Pending",
            StartDate =
                new DateTime(2026, 9, 14),
            EndDate =
                new DateTime(2026, 9, 16),
            RequestedDays = 3,
            CreatedAtUtc =
                new DateTime(
                    2026,
                    8,
                    13,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc),
            CreatedByUserId = 1,
            RowVersion =
            [
                1, 2, 3, 4, 5, 6, 7, 8
            ]
        };
    }

    private static EmployeeLeaveBalanceData
        CreateDefaultLeaveBalance()
    {
        return new EmployeeLeaveBalanceData
        {
            EmployeeLeaveBalanceId = 50,
            EmployeeId = 30,
            IdentificationNumber = "EMP-001",
            FirstName = "Integration",
            LastName = "User",
            EmployeeName = "Integration User",
            DepartmentId = 40,
            DepartmentCode = "HR",
            DepartmentName = "Human Resources",
            LeaveTypeId = 1,
            LeaveTypeCode = "Vacation",
            LeaveTypeName = "Vacation",
            AffectsVacationBalance = true,
            LeavePolicyId = 1,
            LeavePolicyCode =
                "CostaRicaVacationStandard",
            LeavePolicyName =
                "Costa Rica vacation standard",
            EntitlementDays = 12,
            EntitlementWeeks = 50,
            UsesBusinessDays = true,
            AccruedDays = 0,
            AdjustedDays = 12,
            PendingDays = 0,
            UsedDays = 0,
            AvailableDays = 12,
            CreatedAtUtc =
                new DateTime(
                    2026,
                    8,
                    13,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc),
            CreatedByUserId = 1,
            RowVersion =
            [
                1, 2, 3, 4, 5, 6, 7, 8
            ]
        };
    }
}
