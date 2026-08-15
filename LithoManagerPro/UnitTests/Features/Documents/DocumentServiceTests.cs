using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.Documents;
using LithoManager.Application.Features.Documents
    .CreateEmployeeDocument;
using LithoManager.Application.Features.Documents
    .GetEmployeeDocumentById;
using LithoManager.Application.Features.Documents
    .GetEmployeeDocuments;
using LithoManager.Application.Features.Documents
    .SetEmployeeDocumentStatus;
using LithoManager.Application.Features.Documents
    .UpdateEmployeeDocument;
using LithoManager.UnitTests.TestDoubles.Persistence;
using Xunit;

namespace LithoManager.UnitTests.Features.Documents;

public sealed class DocumentServiceTests
{
    private static readonly Guid CorrelationId =
        Guid.Parse(
            "33333333-3333-3333-3333-333333333333");

    private static readonly byte[] RowVersion =
    [
        1, 2, 3, 4, 5, 6, 7, 8
    ];

    private static readonly byte[] FileHash =
        Enumerable
            .Range(1, 32)
            .Select(value => (byte)value)
            .ToArray();

    [Fact]
    public async Task CreateAsync_WhenFileHashIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeDocumentRepository repository = new();

        CreateEmployeeDocumentService service =
            new(repository);

        CreateEmployeeDocumentCommand command =
            CreateValidCreateCommand() with
            {
                FileHash = [1, 2, 3]
            };

        // Act
        EmployeeDocumentResult result =
            await service.CreateAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            DocumentErrorCode.InvalidRequest);

        Assert.Equal(
            0,
            repository.CreateEmployeeDocumentCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_TrimsTextAndCreatesDocument()
    {
        // Arrange
        FakeDocumentRepository repository = new();

        CreateEmployeeDocumentService service =
            new(repository);

        CreateEmployeeDocumentCommand command =
            CreateValidCreateCommand() with
            {
                Title = "  Employment contract  ",
                Description = "  Signed document  ",
                StorageProvider = " LocalFileSystem ",
                StorageKey = " 2026/08/14/document "
            };

        // Act
        EmployeeDocumentResult result =
            await service.CreateAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.EmployeeDocument);
        Assert.Equal(
            "Employment contract",
            repository.LastTitle);
        Assert.Equal(
            "Signed document",
            repository.LastDescription);
        Assert.Equal(
            "LocalFileSystem",
            repository.LastStorageProvider);
        Assert.Equal(
            "2026/08/14/document",
            repository.LastStorageKey);
        Assert.Equal(
            1,
            repository.CreateEmployeeDocumentCallCount);
    }

    [Fact]
    public async Task GetAsync_WhenSearchTermHasSpaces_PassesTrimmedSearchTerm()
    {
        // Arrange
        FakeDocumentRepository repository = new();

        GetEmployeeDocumentsService service =
            new(repository);

        GetEmployeeDocumentsQuery query =
            new(
                ActorUserId: 1,
                EmployeeId: null,
                DocumentTypeId: null,
                IsActive: true,
                IsVisibleToEmployee: null,
                CreatedFromUtc: null,
                CreatedToUtc: null,
                SearchTerm: "  contract  ");

        // Act
        EmployeeDocumentsResult result =
            await service.GetAsync(
                query,
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(
            "contract",
            repository.LastSearchTerm);
        Assert.Equal(
            1,
            repository.GetEmployeeDocumentsCallCount);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDocumentDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        FakeDocumentRepository repository =
            new()
            {
                EmployeeDocumentToReturn = null
            };

        GetEmployeeDocumentByIdService service =
            new(repository);

        // Act
        EmployeeDocumentResult result =
            await service.GetAsync(
                employeeDocumentId: 100,
                actorUserId: 1,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            DocumentErrorCode.EmployeeDocumentNotFound);
        Assert.Equal(
            1,
            repository.GetEmployeeDocumentByIdCallCount);
    }

    [Fact]
    public async Task UpdateAsync_WhenRowVersionIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeDocumentRepository repository = new();

        UpdateEmployeeDocumentService service =
            new(repository);

        UpdateEmployeeDocumentCommand command =
            new(
                EmployeeDocumentId: 100,
                DocumentTypeId: 1,
                Title: "Contract",
                Description: null,
                IssuedDate: null,
                ExpirationDate: null,
                IsVisibleToEmployee: true,
                ExpectedRowVersion: [1, 2, 3],
                ActorUserId: 1,
                RequestContext: CreateRequestContext());

        // Act
        EmployeeDocumentResult result =
            await service.UpdateAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            DocumentErrorCode.InvalidRequest);
        Assert.Equal(
            0,
            repository.UpdateEmployeeDocumentCallCount);
    }

    [Fact]
    public async Task SetStatusAsync_WhenRepositoryReportsConcurrencyConflict_ReturnsConcurrencyConflict()
    {
        // Arrange
        FakeDocumentRepository repository =
            new()
            {
                ExceptionToThrow =
                    CreatePersistenceException(
                        DocumentErrorCode
                            .ConcurrencyConflict)
            };

        SetEmployeeDocumentStatusService service =
            new(repository);

        SetEmployeeDocumentStatusCommand command =
            new(
                EmployeeDocumentId: 100,
                IsActive: false,
                ExpectedRowVersion: RowVersion,
                ActorUserId: 1,
                RequestContext: CreateRequestContext());

        // Act
        EmployeeDocumentResult result =
            await service.SetAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            DocumentErrorCode.ConcurrencyConflict);
        Assert.Equal(
            1,
            repository.SetEmployeeDocumentStatusCallCount);
        Assert.False(repository.LastIsActive);
    }

    private static CreateEmployeeDocumentCommand
        CreateValidCreateCommand()
    {
        return new CreateEmployeeDocumentCommand(
            EmployeeId: 20,
            DocumentTypeId: 1,
            Title: "Employment contract",
            Description: null,
            OriginalFileName: "contract.pdf",
            StorageProvider: "LocalFileSystem",
            StorageKey: "2026/08/14/document",
            ContentType: "application/pdf",
            FileSizeBytes: 128,
            FileHash: FileHash,
            IssuedDate: null,
            ExpirationDate: null,
            IsVisibleToEmployee: true,
            ActorUserId: 1,
            RequestContext: CreateRequestContext());
    }

    private static AuthenticationRequestContext
        CreateRequestContext()
    {
        return new AuthenticationRequestContext(
            CorrelationId,
            ClientIpAddress: "127.0.0.1",
            UserAgent: "LithoManager.UnitTests",
            RequestPath: "/unit-tests/documents");
    }

    private static DocumentPersistenceException
        CreatePersistenceException(
            DocumentErrorCode errorCode)
    {
        return new DocumentPersistenceException(
            errorCode,
            "Persistence error.",
            new InvalidOperationException(
                "Test persistence exception."));
    }

    private static void AssertFailure(
        EmployeeDocumentResult result,
        DocumentErrorCode errorCode)
    {
        Assert.False(result.IsSuccessful);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Null(result.EmployeeDocument);
    }
}
