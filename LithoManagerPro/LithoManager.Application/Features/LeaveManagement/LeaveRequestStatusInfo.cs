namespace LithoManager.Application.Features.LeaveManagement;

public sealed record LeaveRequestStatusInfo(
    string LeaveRequestStatusCode,
    string Name,
    short SortOrder,
    bool IsTerminal,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    byte[] RowVersion);
