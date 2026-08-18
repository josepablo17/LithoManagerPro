namespace LithoManager.Application.Features
    .Payroll.GetAttendanceRecords;

public sealed record GetAttendanceRecordsQuery(
    int ActorUserId,
    int? EmployeeId,
    int? DepartmentId,
    string? AttendanceStatus,
    bool? IsApproved,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? SearchTerm);
