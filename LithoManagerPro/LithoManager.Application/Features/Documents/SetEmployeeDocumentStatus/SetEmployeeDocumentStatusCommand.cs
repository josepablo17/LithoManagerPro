using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Documents.SetEmployeeDocumentStatus;

public sealed record SetEmployeeDocumentStatusCommand(
    int EmployeeDocumentId,
    bool IsActive,
    byte[] ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
