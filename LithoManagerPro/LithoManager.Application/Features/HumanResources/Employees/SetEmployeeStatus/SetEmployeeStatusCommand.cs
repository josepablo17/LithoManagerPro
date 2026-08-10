using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .HumanResources.Employees.SetEmployeeStatus;

public sealed record SetEmployeeStatusCommand(
    int EmployeeId,
    bool IsActive,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
