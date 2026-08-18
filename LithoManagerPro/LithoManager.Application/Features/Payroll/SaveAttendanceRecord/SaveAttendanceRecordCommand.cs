using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Payroll.SaveAttendanceRecord;

public sealed record SaveAttendanceRecordCommand(
    int EmployeeId,
    DateTime? AttendanceDate,
    string? AttendanceStatus,
    decimal? ExpectedHours,
    decimal? WorkedHours,
    decimal? PaidHours,
    decimal? UnpaidHours,
    int? WorkShiftTypeId,
    bool IsPaidHoliday,
    bool IsApproved,
    string? Notes,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
