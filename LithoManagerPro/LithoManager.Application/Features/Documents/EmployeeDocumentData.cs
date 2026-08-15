namespace LithoManager.Application.Features.Documents;

public sealed class EmployeeDocumentData
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

    public int DepartmentId { get; init; }

    public string DepartmentCode { get; init; } =
        string.Empty;

    public string DepartmentName { get; init; } =
        string.Empty;

    public int DocumentTypeId { get; init; }

    public string DocumentTypeCode { get; init; } =
        string.Empty;

    public string DocumentTypeName { get; init; } =
        string.Empty;

    public string Title { get; init; } =
        string.Empty;

    public string? Description { get; init; }

    public string OriginalFileName { get; init; } =
        string.Empty;

    public string ContentType { get; init; } =
        string.Empty;

    public long FileSizeBytes { get; init; }

    public string FileHashAlgorithm { get; init; } =
        string.Empty;

    public DateTime? IssuedDate { get; init; }

    public DateTime? ExpirationDate { get; init; }

    public bool IsVisibleToEmployee { get; init; }

    public bool IsActive { get; init; }

    public DateTime? DeactivatedAtUtc { get; init; }

    public int? DeactivatedByUserId { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public int CreatedByUserId { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public int? UpdatedByUserId { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
