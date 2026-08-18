using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Payroll.CancelOvertimeRecord;

public sealed record CancelOvertimeRecordCommand(
    int OvertimeRecordId,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
