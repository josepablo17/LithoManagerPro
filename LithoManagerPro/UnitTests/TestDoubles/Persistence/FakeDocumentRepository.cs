using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.Documents;

namespace LithoManager.UnitTests.TestDoubles.Persistence;

public sealed class FakeDocumentRepository
    : IDocumentRepository
{
    public DocumentPersistenceException? ExceptionToThrow
    {
        get;
        set;
    }

    public IReadOnlyList<DocumentTypeData> DocumentTypesToReturn
    {
        get;
        set;
    } =
    [
        new DocumentTypeData
        {
            DocumentTypeId = 1,
            DocumentTypeCode = "EmploymentContract",
            Name = "Employment Contract",
            DefaultIsVisibleToEmployee = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            RowVersion = [1, 2, 3, 4, 5, 6, 7, 8]
        }
    ];

    public EmployeeRecordData EmployeeRecordToReturn
    {
        get;
        set;
    } = CreateDefaultEmployeeRecord();

    public IReadOnlyList<EmployeeDocumentData>
        EmployeeDocumentsToReturn
    {
        get;
        set;
    } = [CreateDefaultEmployeeDocument()];

    public EmployeeDocumentData? EmployeeDocumentToReturn
    {
        get;
        set;
    } = CreateDefaultEmployeeDocument();

    public EmployeeDocumentDownloadContextData?
        DownloadContextToReturn
    {
        get;
        set;
    } = CreateDefaultDownloadContext();

    public int CreateEmployeeDocumentCallCount
    {
        get;
        private set;
    }

    public int UpdateEmployeeDocumentCallCount
    {
        get;
        private set;
    }

    public int SetEmployeeDocumentStatusCallCount
    {
        get;
        private set;
    }

    public int GetEmployeeDocumentsCallCount
    {
        get;
        private set;
    }

    public int GetEmployeeDocumentByIdCallCount
    {
        get;
        private set;
    }

    public string? LastTitle
    {
        get;
        private set;
    }

    public string? LastDescription
    {
        get;
        private set;
    }

    public string? LastStorageProvider
    {
        get;
        private set;
    }

    public string? LastStorageKey
    {
        get;
        private set;
    }

    public string? LastSearchTerm
    {
        get;
        private set;
    }

    public byte[]? LastExpectedRowVersion
    {
        get;
        private set;
    }

    public bool? LastIsActive
    {
        get;
        private set;
    }

    public Task<IReadOnlyList<DocumentTypeData>>
        GetDocumentTypesAsync(
            int actorUserId,
            bool? isActive,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfConfigured();

        return Task.FromResult(DocumentTypesToReturn);
    }

    public Task<EmployeeRecordData> EnsureEmployeeRecordAsync(
        int employeeId,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfConfigured();

        return Task.FromResult(EmployeeRecordToReturn);
    }

    public Task<IReadOnlyList<EmployeeDocumentData>>
        GetEmployeeDocumentsAsync(
            int actorUserId,
            int? employeeId,
            int? documentTypeId,
            bool? isActive,
            bool? isVisibleToEmployee,
            DateTime? createdFromUtc,
            DateTime? createdToUtc,
            string? searchTerm,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetEmployeeDocumentsCallCount++;
        LastSearchTerm = searchTerm;

        ThrowIfConfigured();

        return Task.FromResult(EmployeeDocumentsToReturn);
    }

    public Task<EmployeeDocumentData?>
        GetEmployeeDocumentByIdAsync(
            int employeeDocumentId,
            int actorUserId,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetEmployeeDocumentByIdCallCount++;

        ThrowIfConfigured();

        return Task.FromResult(EmployeeDocumentToReturn);
    }

    public Task<EmployeeDocumentDownloadContextData?>
        GetEmployeeDocumentDownloadContextAsync(
            int employeeDocumentId,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfConfigured();

        return Task.FromResult(DownloadContextToReturn);
    }

    public Task<EmployeeDocumentData> CreateEmployeeDocumentAsync(
        int employeeId,
        int documentTypeId,
        string title,
        string? description,
        string originalFileName,
        string storageProvider,
        string storageKey,
        string contentType,
        long fileSizeBytes,
        byte[] fileHash,
        DateTime? issuedDate,
        DateTime? expirationDate,
        bool? isVisibleToEmployee,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CreateEmployeeDocumentCallCount++;
        LastTitle = title;
        LastDescription = description;
        LastStorageProvider = storageProvider;
        LastStorageKey = storageKey;

        ThrowIfConfigured();

        return Task.FromResult(
            EmployeeDocumentToReturn
            ?? CreateDefaultEmployeeDocument());
    }

    public Task<EmployeeDocumentData> UpdateEmployeeDocumentAsync(
        int employeeDocumentId,
        int documentTypeId,
        string title,
        string? description,
        DateTime? issuedDate,
        DateTime? expirationDate,
        bool isVisibleToEmployee,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        UpdateEmployeeDocumentCallCount++;
        LastTitle = title;
        LastDescription = description;
        LastExpectedRowVersion =
            (byte[])expectedRowVersion.Clone();

        ThrowIfConfigured();

        return Task.FromResult(
            EmployeeDocumentToReturn
            ?? CreateDefaultEmployeeDocument());
    }

    public Task<EmployeeDocumentData> SetEmployeeDocumentStatusAsync(
        int employeeDocumentId,
        bool isActive,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SetEmployeeDocumentStatusCallCount++;
        LastIsActive = isActive;
        LastExpectedRowVersion =
            (byte[])expectedRowVersion.Clone();

        ThrowIfConfigured();

        return Task.FromResult(
            EmployeeDocumentToReturn
            ?? CreateDefaultEmployeeDocument());
    }

    private void ThrowIfConfigured()
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }
    }

    private static EmployeeRecordData
        CreateDefaultEmployeeRecord()
    {
        return new EmployeeRecordData
        {
            EmployeeRecordId = 10,
            EmployeeId = 20,
            IdentificationNumber = "DOC-001",
            FirstName = "Document",
            LastName = "User",
            DepartmentId = 30,
            DepartmentCode = "HR",
            DepartmentName = "Human Resources",
            CreatedAtUtc = DateTime.UtcNow,
            RowVersion = [1, 2, 3, 4, 5, 6, 7, 8]
        };
    }

    private static EmployeeDocumentData
        CreateDefaultEmployeeDocument()
    {
        return new EmployeeDocumentData
        {
            EmployeeDocumentId = 100,
            EmployeeRecordId = 10,
            EmployeeId = 20,
            IdentificationNumber = "DOC-001",
            FirstName = "Document",
            LastName = "User",
            DepartmentId = 30,
            DepartmentCode = "HR",
            DepartmentName = "Human Resources",
            DocumentTypeId = 1,
            DocumentTypeCode = "EmploymentContract",
            DocumentTypeName = "Employment Contract",
            Title = "Contract",
            OriginalFileName = "contract.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 128,
            FileHashAlgorithm = "SHA256",
            IsVisibleToEmployee = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = 1,
            RowVersion = [1, 2, 3, 4, 5, 6, 7, 8]
        };
    }

    private static EmployeeDocumentDownloadContextData
        CreateDefaultDownloadContext()
    {
        return new EmployeeDocumentDownloadContextData
        {
            EmployeeDocumentId = 100,
            EmployeeRecordId = 10,
            EmployeeId = 20,
            IdentificationNumber = "DOC-001",
            FirstName = "Document",
            LastName = "User",
            DocumentTypeId = 1,
            DocumentTypeCode = "EmploymentContract",
            DocumentTypeName = "Employment Contract",
            Title = "Contract",
            OriginalFileName = "contract.pdf",
            StorageProvider = "LocalFileSystem",
            StorageKey = "2026/08/14/test",
            ContentType = "application/pdf",
            FileSizeBytes = 128,
            FileHash = new byte[32],
            FileHashAlgorithm = "SHA256",
            IsVisibleToEmployee = true,
            IsActive = true,
            RowVersion = [1, 2, 3, 4, 5, 6, 7, 8]
        };
    }
}
