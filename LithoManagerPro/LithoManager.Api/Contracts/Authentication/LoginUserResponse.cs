namespace LithoManager.Api.Contracts.Authentication;

public sealed record LoginUserResponse(
    int UserId,
    string EmailAddress,
    string RoleCode,
    string RoleDisplayName,
    int? EmployeeId,
    string? FirstName,
    string? LastName,
    string? JobTitle,
    string? ProfileImagePath,
    int? DepartmentId,
    string? DepartmentCode,
    string? DepartmentName);