namespace LithoManager.Application.Features.Authentication.Login;

public sealed record LoginUserData(
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