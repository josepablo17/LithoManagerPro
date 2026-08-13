namespace LithoManager.Application.Features
    .LeaveManagement.GetLeaveRequests;

public sealed record GetLeaveRequestsQuery(
    int ActorUserId,
    string? LeaveRequestStatusCode,
    int? EmployeeId,
    int? DepartmentId,
    DateTime? StartDateFrom,
    DateTime? StartDateTo,
    string? SearchTerm);
