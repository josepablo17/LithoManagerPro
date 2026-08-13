namespace LithoManager.Application.Features.LeaveManagement;

public sealed class LeaveTypeData
{
    public int LeaveTypeId { get; init; }

    public string LeaveTypeCode { get; init; } =
        string.Empty;

    public string Name { get; init; } =
        string.Empty;

    public bool AffectsVacationBalance { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public int? CreatedByUserId { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public int? UpdatedByUserId { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
