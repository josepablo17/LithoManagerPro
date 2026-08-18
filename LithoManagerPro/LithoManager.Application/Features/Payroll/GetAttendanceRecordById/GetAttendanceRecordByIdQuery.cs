namespace LithoManager.Application.Features
    .Payroll.GetAttendanceRecordById;

public sealed record GetAttendanceRecordByIdQuery(
    int AttendanceRecordId,
    int ActorUserId);
