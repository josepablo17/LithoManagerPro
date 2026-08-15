using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Documents.GetEmployeeDocumentDownloadContext;

public sealed record GetEmployeeDocumentDownloadContextCommand(
    int EmployeeDocumentId,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
