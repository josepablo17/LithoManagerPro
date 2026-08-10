using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .HumanResources.Departments.CreateDepartment;

public sealed record CreateDepartmentCommand(
    string? DepartmentCode,
    string? Name,
    string? Description,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
