using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.LeaveManagement;

namespace LithoManager.Application.Abstractions.Persistence;

public interface ILeaveManagementRepository
{
    Task<IReadOnlyList<LeaveTypeData>> GetLeaveTypesAsync(
        bool? isActive,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaveRequestStatusData>>
        GetLeaveRequestStatusesAsync(
            bool? isActive,
            CancellationToken cancellationToken);

    Task<EmployeeLeaveBalanceData?> GetEmployeeLeaveBalanceAsync(
        int? employeeId,
        string leaveTypeCode,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<EmployeeLeaveBalanceData> AdjustEmployeeLeaveBalanceAsync(
        int employeeId,
        string leaveTypeCode,
        decimal adjustedDaysDelta,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaveRequestData>> GetMyLeaveRequestsAsync(
        int actorUserId,
        string? leaveRequestStatusCode,
        DateTime? startDateFrom,
        DateTime? startDateTo,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaveRequestData>> GetLeaveRequestsAsync(
        int actorUserId,
        string? leaveRequestStatusCode,
        int? employeeId,
        int? departmentId,
        DateTime? startDateFrom,
        DateTime? startDateTo,
        string? searchTerm,
        CancellationToken cancellationToken);

    Task<LeaveRequestData?> GetLeaveRequestByIdAsync(
        int leaveRequestId,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<LeaveRequestData> CreateLeaveRequestAsync(
        DateTime startDate,
        DateTime endDate,
        int actorUserId,
        string leaveTypeCode,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<LeaveRequestData> CancelLeaveRequestAsync(
        int leaveRequestId,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<LeaveRequestData> RespondLeaveRequestAsync(
        int leaveRequestId,
        bool isApproved,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);
}
