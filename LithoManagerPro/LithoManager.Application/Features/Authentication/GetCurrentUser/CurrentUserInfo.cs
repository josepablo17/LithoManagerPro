namespace LithoManager.Application.Features.Authentication
    .GetCurrentUser;

public sealed record CurrentUserInfo(
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