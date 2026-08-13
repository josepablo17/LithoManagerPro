namespace LithoManager.Application.Features.LeaveManagement;

public sealed record LeaveTypeInfo(
    int LeaveTypeId,
    string LeaveTypeCode,
    string Name,
    bool AffectsVacationBalance,
    bool IsActive,
    DateTime CreatedAtUtc,
    int? CreatedByUserId,
    DateTime? UpdatedAtUtc,
    int? UpdatedByUserId,
    byte[] RowVersion);
