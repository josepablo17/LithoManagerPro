namespace LithoManager.Application.Features.Documents;

public sealed record EmployeeDocumentDownloadContextInfo(
    int EmployeeDocumentId,
    int EmployeeRecordId,
    int EmployeeId,
    string IdentificationNumber,
    string FirstName,
    string LastName,
    int DocumentTypeId,
    string DocumentTypeCode,
    string DocumentTypeName,
    string Title,
    string OriginalFileName,
    string StorageProvider,
    string StorageKey,
    string ContentType,
    long FileSizeBytes,
    byte[] FileHash,
    string FileHashAlgorithm,
    bool IsVisibleToEmployee,
    bool IsActive,
    byte[] RowVersion);
