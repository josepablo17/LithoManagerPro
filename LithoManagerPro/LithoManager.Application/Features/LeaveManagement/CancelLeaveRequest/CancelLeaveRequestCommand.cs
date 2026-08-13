using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .LeaveManagement.CancelLeaveRequest;

public sealed record CancelLeaveRequestCommand(
    int LeaveRequestId,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
