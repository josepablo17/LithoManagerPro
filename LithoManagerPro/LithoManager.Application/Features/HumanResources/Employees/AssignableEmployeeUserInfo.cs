namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed record AssignableEmployeeUserInfo(
    int UserId,
    string EmailAddress,
    int RoleId,
    string RoleCode,
    string RoleName,
    int? AssignedEmployeeId,
    string? AssignedEmployeeFirstName,
    string? AssignedEmployeeLastName);
