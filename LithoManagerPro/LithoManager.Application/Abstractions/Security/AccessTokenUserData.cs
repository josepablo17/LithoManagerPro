namespace LithoManager.Application.Abstractions.Security;

public sealed record AccessTokenUserData(
    int UserId,
    string EmailAddress,
    string RoleCode,
    int? EmployeeId);