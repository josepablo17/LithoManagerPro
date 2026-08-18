using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Payroll.RespondOvertimeRecord;

public sealed record RespondOvertimeRecordCommand(
    int OvertimeRecordId,
    bool IsApproved,
    string? RejectionReason,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
