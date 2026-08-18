namespace LithoManager.Application.Features
    .Payroll.GetOvertimeRecordById;

public sealed record GetOvertimeRecordByIdQuery(
    int OvertimeRecordId,
    int ActorUserId);
