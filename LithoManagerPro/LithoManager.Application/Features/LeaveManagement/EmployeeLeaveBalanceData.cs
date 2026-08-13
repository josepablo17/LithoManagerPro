namespace LithoManager.Application.Features.LeaveManagement;

public sealed class EmployeeLeaveBalanceData
{
    public int EmployeeLeaveBalanceId { get; init; }

    public int EmployeeId { get; init; }

    public string? IdentificationNumber { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? EmployeeName { get; init; }

    public int? DepartmentId { get; init; }

    public string? DepartmentCode { get; init; }

    public string? DepartmentName { get; init; }

    public int LeaveTypeId { get; init; }

    public string LeaveTypeCode { get; init; } =
        string.Empty;

    public string? LeaveTypeName { get; init; }

    public bool AffectsVacationBalance { get; init; }

    public int LeavePolicyId { get; init; }

    public string? LeavePolicyCode { get; init; }

    public string? LeavePolicyName { get; init; }

    public decimal EntitlementDays { get; init; }

    public short EntitlementWeeks { get; init; }

    public bool UsesBusinessDays { get; init; }

    public decimal AccruedDays { get; init; }

    public decimal AdjustedDays { get; init; }

    public decimal PendingDays { get; init; }

    public decimal UsedDays { get; init; }

    public decimal AvailableDays { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public int? CreatedByUserId { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public int? UpdatedByUserId { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
