namespace LithoManager.Application.Abstractions.Security;

public sealed record AccessTokenUserData(
    int UserId,
    string EmailAddress,
    int TokenVersion,
    string RoleCode,
    int? EmployeeId);
