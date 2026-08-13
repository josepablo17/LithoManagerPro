namespace LithoManager.Application.Features
    .LeaveManagement.GetMyLeaveRequests;

public sealed record GetMyLeaveRequestsQuery(
    int ActorUserId,
    string? LeaveRequestStatusCode,
    DateTime? StartDateFrom,
    DateTime? StartDateTo);
