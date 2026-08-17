namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed record EmployeeSalaryHistoryInfo(
    int EmployeeSalaryHistoryId,
    int EmployeeId,
    string IdentificationType,
    string IdentificationNumber,
    string FirstName,
    string LastName,
    int DepartmentId,
    string DepartmentCode,
    string DepartmentName,
    decimal BaseSalary,
    DateTime EffectiveFromDate,
    DateTime? EffectiveToDate,
    bool IsCurrent,
    DateTime CreatedAtUtc,
    int? CreatedByUserId,
    DateTime? UpdatedAtUtc,
    int? UpdatedByUserId,
    byte[] RowVersion);
