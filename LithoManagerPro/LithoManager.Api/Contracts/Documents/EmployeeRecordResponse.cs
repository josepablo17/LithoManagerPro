namespace LithoManager.Api.Contracts.Documents;

public sealed record EmployeeRecordResponse(
    int EmployeeRecordId,
    int EmployeeId,
    string IdentificationNumber,
    string FirstName,
    string LastName,
    int DepartmentId,
    string DepartmentCode,
    string DepartmentName,
    DateTime CreatedAtUtc,
    int? CreatedByUserId,
    DateTime? UpdatedAtUtc,
    int? UpdatedByUserId,
    string RowVersion);
