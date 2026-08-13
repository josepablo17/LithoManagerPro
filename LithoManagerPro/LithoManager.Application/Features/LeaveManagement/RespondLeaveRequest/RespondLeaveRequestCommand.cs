using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .LeaveManagement.RespondLeaveRequest;

public sealed record RespondLeaveRequestCommand(
    int LeaveRequestId,
    bool IsApproved,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
