namespace LithoManager.Api.Contracts
    .HumanResources.Departments;

public sealed record DepartmentResponse(
    int DepartmentId,
    string DepartmentCode,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    int? CreatedByUserId,
    DateTime? UpdatedAtUtc,
    int? UpdatedByUserId,
    string RowVersion);
