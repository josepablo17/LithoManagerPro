using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Documents.UpdateEmployeeDocument;

public sealed record UpdateEmployeeDocumentCommand(
    int EmployeeDocumentId,
    int DocumentTypeId,
    string Title,
    string? Description,
    DateTime? IssuedDate,
    DateTime? ExpirationDate,
    bool IsVisibleToEmployee,
    byte[] ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
