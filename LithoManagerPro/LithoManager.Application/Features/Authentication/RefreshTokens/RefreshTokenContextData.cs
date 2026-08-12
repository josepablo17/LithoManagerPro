namespace LithoManager.Application.Features.Authentication
    .RefreshTokens;

public sealed class RefreshTokenContextData
{
    public int RefreshTokenId { get; set; }

    public int UserId { get; set; }

    public Guid TokenFamilyId { get; set; }

    public int RefreshTokenVersion { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    public string EmailAddress { get; set; } =
        string.Empty;

    public int TokenVersion { get; set; }

    public bool IsEmailConfirmed { get; set; }

    public bool IsActive { get; set; }

    public bool RequiresPasswordChange { get; set; }

    public int RoleId { get; set; }

    public string RoleCode { get; set; } =
        string.Empty;

    public string RoleDisplayName { get; set; } =
        string.Empty;

    public bool IsRoleActive { get; set; }

    public int? EmployeeId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? JobTitle { get; set; }

    public string? ProfileImagePath { get; set; }

    public bool? IsEmployeeActive { get; set; }

    public int? DepartmentId { get; set; }

    public string? DepartmentCode { get; set; }

    public string? DepartmentName { get; set; }

    public bool? IsDepartmentActive { get; set; }
}
