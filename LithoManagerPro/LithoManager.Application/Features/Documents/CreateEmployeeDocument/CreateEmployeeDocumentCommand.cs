using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .Documents.CreateEmployeeDocument;

public sealed record CreateEmployeeDocumentCommand(
    int EmployeeId,
    int DocumentTypeId,
    string Title,
    string? Description,
    string OriginalFileName,
    string StorageProvider,
    string StorageKey,
    string ContentType,
    long FileSizeBytes,
    byte[] FileHash,
    DateTime? IssuedDate,
    DateTime? ExpirationDate,
    bool? IsVisibleToEmployee,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
