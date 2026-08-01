namespace LithoManager.Application.Features.Authentication
    .GetCurrentUser;

public sealed class CurrentUserData
{
    public int UserId { get; init; }

    public string EmailAddress { get; init; } =
        string.Empty;

    public bool IsEmailConfirmed { get; init; }

    public bool IsActive { get; init; }

    public bool RequiresPasswordChange { get; init; }

    public string RoleCode { get; init; } =
        string.Empty;

    public string RoleDisplayName { get; init; } =
        string.Empty;

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

    public bool? IsDepartmentActive { get; init; }
}