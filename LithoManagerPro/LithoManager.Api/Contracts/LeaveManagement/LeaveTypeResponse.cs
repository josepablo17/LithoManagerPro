namespace LithoManager.Api.Contracts.LeaveManagement;

public sealed record LeaveTypeResponse(
    int LeaveTypeId,
    string LeaveTypeCode,
    string Name,
    bool AffectsVacationBalance,
    bool IsActive,
    DateTime CreatedAtUtc,
    int? CreatedByUserId,
    DateTime? UpdatedAtUtc,
    int? UpdatedByUserId,
    string RowVersion);
