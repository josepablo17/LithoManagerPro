using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .HumanResources.Departments.UpdateDepartment;

public sealed record UpdateDepartmentCommand(
    int DepartmentId,
    string? DepartmentCode,
    string? Name,
    string? Description,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
