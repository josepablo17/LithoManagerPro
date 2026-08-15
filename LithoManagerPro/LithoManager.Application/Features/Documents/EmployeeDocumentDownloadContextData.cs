namespace LithoManager.Application.Features.Documents;

public sealed class EmployeeDocumentDownloadContextData
{
    public int EmployeeDocumentId { get; init; }

    public int EmployeeRecordId { get; init; }

    public int EmployeeId { get; init; }

    public string IdentificationNumber { get; init; } =
        string.Empty;

    public string FirstName { get; init; } =
        string.Empty;

    public string LastName { get; init; } =
        string.Empty;

    public int DocumentTypeId { get; init; }

    public string DocumentTypeCode { get; init; } =
        string.Empty;

    public string DocumentTypeName { get; init; } =
        string.Empty;

    public string Title { get; init; } =
        string.Empty;

    public string OriginalFileName { get; init; } =
        string.Empty;

    public string StorageProvider { get; init; } =
        string.Empty;

    public string StorageKey { get; init; } =
        string.Empty;

    public string ContentType { get; init; } =
        string.Empty;

    public long FileSizeBytes { get; init; }

    public byte[] FileHash { get; init; } = [];

    public string FileHashAlgorithm { get; init; } =
        string.Empty;

    public bool IsVisibleToEmployee { get; init; }

    public bool IsActive { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
