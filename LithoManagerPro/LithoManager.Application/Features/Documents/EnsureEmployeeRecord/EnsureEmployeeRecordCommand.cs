using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Documents.EnsureEmployeeRecord;

public sealed record EnsureEmployeeRecordCommand(
    int EmployeeId,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
