namespace LithoManager.Application.Features.LeaveManagement;

public sealed class LeaveRequestStatusData
{
    public string LeaveRequestStatusCode { get; init; } =
        string.Empty;

    public string Name { get; init; } =
        string.Empty;

    public short SortOrder { get; init; }

    public bool IsTerminal { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
