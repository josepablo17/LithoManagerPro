using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .LeaveManagement.CreateLeaveRequest;

public sealed record CreateLeaveRequestCommand(
    DateTime? StartDate,
    DateTime? EndDate,
    int ActorUserId,
    string? LeaveTypeCode,
    AuthenticationRequestContext RequestContext);
