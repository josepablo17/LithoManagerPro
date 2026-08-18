namespace LithoManager.Api.Contracts.Payroll;

public sealed class SetEmployeeWorkScheduleRequest
{
    public int? EmployeeId { get; init; }
    public int? WorkShiftTypeId { get; init; }
    public DateTime? EffectiveFromDate { get; init; }
}

public sealed class SaveAttendanceRecordRequest
{
    public int? EmployeeId { get; init; }
    public DateTime? AttendanceDate { get; init; }
    public string? AttendanceStatus { get; init; }
    public decimal? ExpectedHours { get; init; }
    public decimal? WorkedHours { get; init; }
    public decimal? PaidHours { get; init; }
    public decimal? UnpaidHours { get; init; }
    public int? WorkShiftTypeId { get; init; }
    public bool IsPaidHoliday { get; init; }
    public bool IsApproved { get; init; }
    public string? Notes { get; init; }
    public string? ExpectedRowVersion { get; init; }
}

public sealed class CreateOvertimeRecordRequest
{
    public int? EmployeeId { get; init; }
    public int? OvertimeRuleId { get; init; }
    public DateTime? OvertimeDate { get; init; }
    public decimal? Hours { get; init; }
    public int? AttendanceRecordId { get; init; }
    public string? Notes { get; init; }
}

public sealed class RespondOvertimeRecordRequest
{
    public bool IsApproved { get; init; }
    public string? RejectionReason { get; init; }
    public string? ExpectedRowVersion { get; init; }
}

public sealed class CancelOvertimeRecordRequest
{
    public string? ExpectedRowVersion { get; init; }
}

public sealed class CreateEmployeeDisabilityRequest
{
    public int? EmployeeId { get; init; }
    public int? DisabilityTypeId { get; init; }
    public string? IssuerInstitution { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? ReferenceNumber { get; init; }
    public decimal? EmployerPaidAmount { get; init; }
    public decimal? SubsidyAmount { get; init; }
    public string? Notes { get; init; }
}

public sealed class ApproveEmployeeDisabilityRequest
{
    public string? ExpectedRowVersion { get; init; }
}

public sealed class CancelEmployeeDisabilityRequest
{
    public string? CancellationReason { get; init; }
    public string? ExpectedRowVersion { get; init; }
}
