using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .LeaveManagement.AdjustEmployeeLeaveBalance;

public sealed record AdjustEmployeeLeaveBalanceCommand(
    int EmployeeId,
    string? LeaveTypeCode,
    decimal AdjustedDaysDelta,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
