using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Payroll.SetEmployeeWorkSchedule;

public sealed record SetEmployeeWorkScheduleCommand(
    int EmployeeId,
    int WorkShiftTypeId,
    DateTime? EffectiveFromDate,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
