namespace LithoManager.Application.Features
    .HumanResources.Departments;

public sealed record DepartmentInfo(
    int DepartmentId,
    string DepartmentCode,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    int? CreatedByUserId,
    DateTime? UpdatedAtUtc,
    int? UpdatedByUserId,
    byte[] RowVersion);
