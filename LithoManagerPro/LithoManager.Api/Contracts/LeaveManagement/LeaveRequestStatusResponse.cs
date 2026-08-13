namespace LithoManager.Api.Contracts.LeaveManagement;

public sealed record LeaveRequestStatusResponse(
    string LeaveRequestStatusCode,
    string Name,
    short SortOrder,
    bool IsTerminal,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string RowVersion);
