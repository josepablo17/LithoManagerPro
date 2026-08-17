namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed record EmployeeInfo(
    int EmployeeId,
    int? UserId,
    string? EmailAddress,
    int DepartmentId,
    string DepartmentCode,
    string DepartmentName,
    bool IsDepartmentActive,
    string IdentificationType,
    string IdentificationNumber,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    DateTime? BirthDate,
    DateTime HireDate,
    DateTime? TerminationDate,
    string JobTitle,
    decimal BaseSalary,
    string? ProfileImagePath,
    bool IsActive,
    DateTime CreatedAtUtc,
    int? CreatedByUserId,
    DateTime? UpdatedAtUtc,
    int? UpdatedByUserId,
    byte[] RowVersion);
