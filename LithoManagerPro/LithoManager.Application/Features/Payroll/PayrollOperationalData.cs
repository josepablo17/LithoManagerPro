namespace LithoManager.Application.Features.Payroll;

public sealed class EmployeeWorkScheduleData
{
    public int EmployeeWorkScheduleId { get; init; }
    public int EmployeeId { get; init; }
    public string IdentificationType { get; init; } = string.Empty;
    public string IdentificationNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int WorkShiftTypeId { get; init; }
    public string WorkShiftTypeCode { get; init; } = string.Empty;
    public string WorkShiftTypeName { get; init; } = string.Empty;
    public decimal WeeklyOrdinaryHours { get; init; }
    public bool WorksMonday { get; init; }
    public bool WorksTuesday { get; init; }
    public bool WorksWednesday { get; init; }
    public bool WorksThursday { get; init; }
    public bool WorksFriday { get; init; }
    public bool WorksSaturday { get; init; }
    public bool WorksSunday { get; init; }
    public DateTime EffectiveFromDate { get; init; }
    public DateTime? EffectiveToDate { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class AttendanceRecordData
{
    public int AttendanceRecordId { get; init; }
    public int EmployeeId { get; init; }
    public string IdentificationType { get; init; } = string.Empty;
    public string IdentificationNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int WorkShiftTypeId { get; init; }
    public string WorkShiftTypeCode { get; init; } = string.Empty;
    public string WorkShiftTypeName { get; init; } = string.Empty;
    public DateTime AttendanceDate { get; init; }
    public string AttendanceStatus { get; init; } = string.Empty;
    public decimal ExpectedHours { get; init; }
    public decimal WorkedHours { get; init; }
    public decimal PaidHours { get; init; }
    public decimal UnpaidHours { get; init; }
    public bool IsPaidHoliday { get; init; }
    public bool IsApproved { get; init; }
    public DateTime? ApprovedAtUtc { get; init; }
    public int? ApprovedByUserId { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class OvertimeRecordData
{
    public int OvertimeRecordId { get; init; }
    public int EmployeeId { get; init; }
    public string IdentificationType { get; init; } = string.Empty;
    public string IdentificationNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int? AttendanceRecordId { get; init; }
    public int OvertimeRuleId { get; init; }
    public string OvertimeRuleCode { get; init; } = string.Empty;
    public string OvertimeRuleName { get; init; } = string.Empty;
    public decimal HourMultiplier { get; init; }
    public DateTime OvertimeDate { get; init; }
    public decimal Hours { get; init; }
    public string ApprovalStatus { get; init; } = string.Empty;
    public DateTime? ApprovedAtUtc { get; init; }
    public int? ApprovedByUserId { get; init; }
    public DateTime? RejectedAtUtc { get; init; }
    public int? RejectedByUserId { get; init; }
    public string? RejectionReason { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class EmployeeDisabilityData
{
    public int EmployeeDisabilityId { get; init; }
    public int EmployeeId { get; init; }
    public string IdentificationType { get; init; } = string.Empty;
    public string IdentificationNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int DisabilityTypeId { get; init; }
    public string DisabilityTypeCode { get; init; } = string.Empty;
    public string DisabilityTypeName { get; init; } = string.Empty;
    public bool CountsAsSalaryForAguinaldo { get; init; }
    public bool RequiresSubsidyTracking { get; init; }
    public bool ReducesWorkedDays { get; init; }
    public string IssuerInstitution { get; init; } = string.Empty;
    public string? ReferenceNumber { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime ReportedDate { get; init; }
    public string DisabilityStatus { get; init; } = string.Empty;
    public decimal? EmployerPaidAmount { get; init; }
    public decimal? SubsidyAmount { get; init; }
    public DateTime? ApprovedAtUtc { get; init; }
    public int? ApprovedByUserId { get; init; }
    public DateTime? CancelledAtUtc { get; init; }
    public int? CancelledByUserId { get; init; }
    public string? CancellationReason { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}
