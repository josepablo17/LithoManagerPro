using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .HumanResources.Departments.SetDepartmentStatus;

public sealed record SetDepartmentStatusCommand(
    int DepartmentId,
    bool IsActive,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
