namespace LithoManager.Application.Features
    .LeaveManagement.GetEmployeeLeaveBalance;

public sealed record GetEmployeeLeaveBalanceQuery(
    int? EmployeeId,
    string? LeaveTypeCode,
    int ActorUserId);
