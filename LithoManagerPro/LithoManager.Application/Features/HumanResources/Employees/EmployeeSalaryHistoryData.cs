namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed class EmployeeSalaryHistoryData
{
    public int EmployeeSalaryHistoryId { get; init; }

    public int EmployeeId { get; init; }

    public string IdentificationType { get; init; } =
        string.Empty;

    public string IdentificationNumber { get; init; } =
        string.Empty;

    public string FirstName { get; init; } =
        string.Empty;

    public string LastName { get; init; } =
        string.Empty;

    public int DepartmentId { get; init; }

    public string DepartmentCode { get; init; } =
        string.Empty;

    public string DepartmentName { get; init; } =
        string.Empty;

    public decimal BaseSalary { get; init; }

    public DateTime EffectiveFromDate { get; init; }

    public DateTime? EffectiveToDate { get; init; }

    public bool IsCurrent { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public int? CreatedByUserId { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public int? UpdatedByUserId { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
