namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed class EmployeeData
{
    public int EmployeeId { get; init; }

    public int? UserId { get; init; }

    public string? EmailAddress { get; init; }

    public int DepartmentId { get; init; }

    public string DepartmentCode { get; init; } =
        string.Empty;

    public string DepartmentName { get; init; } =
        string.Empty;

    public bool IsDepartmentActive { get; init; }

    public string IdentificationNumber { get; init; } =
        string.Empty;

    public string FirstName { get; init; } =
        string.Empty;

    public string LastName { get; init; } =
        string.Empty;

    public string? PhoneNumber { get; init; }

    public DateTime? BirthDate { get; init; }

    public DateTime HireDate { get; init; }

    public DateTime? TerminationDate { get; init; }

    public string JobTitle { get; init; } =
        string.Empty;

    public decimal BaseSalary { get; init; }

    public string? ProfileImagePath { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public int? CreatedByUserId { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public int? UpdatedByUserId { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
