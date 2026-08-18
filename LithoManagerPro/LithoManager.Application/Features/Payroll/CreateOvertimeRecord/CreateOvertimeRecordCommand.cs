using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Payroll.CreateOvertimeRecord;

public sealed record CreateOvertimeRecordCommand(
    int EmployeeId,
    int OvertimeRuleId,
    DateTime? OvertimeDate,
    decimal? Hours,
    int? AttendanceRecordId,
    string? Notes,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
