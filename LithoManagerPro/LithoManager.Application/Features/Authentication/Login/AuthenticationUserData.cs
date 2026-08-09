namespace LithoManager.Application.Features.Authentication.Login;

public sealed class AuthenticationUserData
{
    public int UserId { get; init; }

    public string EmailAddress { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public int TokenVersion { get; init; }

    public bool IsEmailConfirmed { get; init; }

    public bool IsActive { get; init; }

    public bool RequiresPasswordChange { get; init; }

    public DateTime? TemporaryPasswordExpiresAtUtc { get; init; }

    public DateTime? PasswordChangedAtUtc { get; init; }

    public short FailedLoginAttempts { get; init; }

    public DateTime? LockoutEndAtUtc { get; init; }

    public DateTime? LastLoginAtUtc { get; init; }

    public int RoleId { get; init; }

    public string RoleCode { get; init; } = string.Empty;

    public string RoleDisplayName { get; init; } = string.Empty;

    public bool IsRoleActive { get; init; }

    public int? EmployeeId { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? JobTitle { get; init; }

    public string? ProfileImagePath { get; init; }

    public bool? IsEmployeeActive { get; init; }

    public int? DepartmentId { get; init; }

    public string? DepartmentCode { get; init; }

    public string? DepartmentName { get; init; }
}
