namespace LithoManager.Api.Contracts
    .HumanResources.Employees;

public sealed record AssignableEmployeeUserResponse(
    int UserId,
    string EmailAddress,
    int RoleId,
    string RoleCode,
    string RoleName,
    int? AssignedEmployeeId,
    string? AssignedEmployeeFirstName,
    string? AssignedEmployeeLastName);
