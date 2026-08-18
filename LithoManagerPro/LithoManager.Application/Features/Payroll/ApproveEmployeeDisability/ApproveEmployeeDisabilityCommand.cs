using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Payroll.ApproveEmployeeDisability;

public sealed record ApproveEmployeeDisabilityCommand(
    int EmployeeDisabilityId,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
