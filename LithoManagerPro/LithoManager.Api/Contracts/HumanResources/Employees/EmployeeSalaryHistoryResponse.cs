namespace LithoManager.Api.Contracts
    .HumanResources.Employees;

public sealed record EmployeeSalaryHistoryResponse(
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
    string RowVersion);
