using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Payroll.CancelEmployeeDisability;

public sealed record CancelEmployeeDisabilityCommand(
    int EmployeeDisabilityId,
    string? CancellationReason,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
