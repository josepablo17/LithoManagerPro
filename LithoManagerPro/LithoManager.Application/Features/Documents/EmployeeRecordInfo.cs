namespace LithoManager.Application.Features.Documents;

public sealed record EmployeeRecordInfo(
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
    byte[] RowVersion);
