namespace LithoManager.Application.Features.LeaveManagement;

public sealed class LeaveRequestData
{
    public int LeaveRequestId { get; init; }

    public int EmployeeId { get; init; }

    public string IdentificationNumber { get; init; } =
        string.Empty;

    public string FirstName { get; init; } =
        string.Empty;

    public string LastName { get; init; } =
        string.Empty;

    public int DepartmentId { get; init; }

    public string DepartmentCode { get; init; } =
        string.Empty;

    public string DepartmentName { get; init; } =
        string.Empty;

    public int LeaveTypeId { get; init; }

    public string LeaveTypeCode { get; init; } =
        string.Empty;

    public string LeaveTypeName { get; init; } =
        string.Empty;

    public string LeaveRequestStatusCode { get; init; } =
        string.Empty;

    public string LeaveRequestStatusName { get; init; } =
        string.Empty;

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public decimal RequestedDays { get; init; }

    public DateTime? RespondedAtUtc { get; init; }

    public int? RespondedByUserId { get; init; }

    public string? RespondedByEmailAddress { get; init; }

    public DateTime? CancelledAtUtc { get; init; }

    public int? CancelledByUserId { get; init; }

    public string? CancelledByEmailAddress { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public int CreatedByUserId { get; init; }

    public string? CreatedByEmailAddress { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public int? UpdatedByUserId { get; init; }

    public string? UpdatedByEmailAddress { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
