using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Departments;
using LithoManager.Application.Features.Documents;
using LithoManager.IntegrationTests.Collections;
using LithoManager.IntegrationTests.Fixtures;
using Xunit;

namespace LithoManager.IntegrationTests.Infrastructure
    .Persistence;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class DocumentRepositoryTests
{
    private readonly AuthenticationDatabaseFixture
        _fixture;

    public DocumentRepositoryTests(
        AuthenticationDatabaseFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _fixture = fixture;
    }

    [Fact]
    public async Task CreateEmployeeDocumentAsync_WhenRequestIsValid_PersistsDocumentAndAudit()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

        Guid correlationId =
            Guid.NewGuid();

        await CleanupAsync(
            departmentCode,
            identificationNumber);

        try
        {
            int employeeId =
                await CreateEmployeeAsync(
                    departmentCode,
                    identificationNumber);

            DocumentTypeData documentType =
                await GetEmploymentContractTypeAsync();

            string storageKey =
                CreateStorageKey();

            // Act
            EmployeeDocumentData document =
                await _fixture.DocumentRepository
                    .CreateEmployeeDocumentAsync(
                        employeeId:
                            employeeId,
                        documentTypeId:
                            documentType.DocumentTypeId,
                        title:
                            "Employment contract",
                        description:
                            "Created by integration tests.",
                        originalFileName:
                            "contract.pdf",
                        storageProvider:
                            "IntegrationTests",
                        storageKey:
                            storageKey,
                        contentType:
                            "application/pdf",
                        fileSizeBytes:
                            128,
                        fileHash:
                            CreateFileHash(),
                        issuedDate:
                            new DateTime(2026, 8, 14),
                        expirationDate:
                            null,
                        isVisibleToEmployee:
                            true,
                        actorUserId:
                            _fixture
                                .SuperAdministratorUserId,
                        requestContext:
                            CreateRequestContext(
                                correlationId,
                                "/integration-tests/" +
                                "documents/create"),
                        cancellationToken:
                            CancellationToken.None);

            // Assert
            Assert.True(
                document.EmployeeDocumentId > 0);
            Assert.Equal(
                employeeId,
                document.EmployeeId);
            Assert.Equal(
                documentType.DocumentTypeId,
                document.DocumentTypeId);
            Assert.Equal(
                "Employment contract",
                document.Title);
            Assert.True(document.IsVisibleToEmployee);
            Assert.True(document.IsActive);
            Assert.Equal(
                8,
                document.RowVersion.Length);

            EmployeeDocumentData? persisted =
                await _fixture.DocumentRepository
                    .GetEmployeeDocumentByIdAsync(
                        document.EmployeeDocumentId,
                        _fixture
                            .SuperAdministratorUserId,
                        CancellationToken.None);

            Assert.NotNull(persisted);
            Assert.Equal(
                document.EmployeeDocumentId,
                persisted.EmployeeDocumentId);

            EmployeeDocumentDownloadContextData? downloadContext =
                await _fixture.DocumentRepository
                    .GetEmployeeDocumentDownloadContextAsync(
                        document.EmployeeDocumentId,
                        _fixture
                            .SuperAdministratorUserId,
                        CreateRequestContext(
                            Guid.NewGuid(),
                            "/integration-tests/" +
                            "documents/download-context"),
                        CancellationToken.None);

            Assert.NotNull(downloadContext);
            Assert.Equal(
                storageKey,
                downloadContext.StorageKey);

            AuditLogTestData? audit =
                await _fixture
                    .GetAuditLogByCorrelationIdAsync(
                        correlationId);

            Assert.NotNull(audit);
            Assert.Equal(
                "Documents",
                audit.ModuleName);
            Assert.Equal(
                "EmployeeDocumentCreated",
                audit.ActionName);
            Assert.Equal(
                document.EmployeeDocumentId.ToString(),
                audit.EntityId);
        }
        finally
        {
            await CleanupAsync(
                departmentCode,
                identificationNumber);
        }
    }

    [Fact]
    public async Task SetEmployeeDocumentStatusAsync_WhenDocumentIsActive_DeactivatesDocument()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

        await CleanupAsync(
            departmentCode,
            identificationNumber);

        try
        {
            int employeeId =
                await CreateEmployeeAsync(
                    departmentCode,
                    identificationNumber);

            DocumentTypeData documentType =
                await GetEmploymentContractTypeAsync();

            EmployeeDocumentData document =
                await CreateDocumentAsync(
                    employeeId,
                    documentType.DocumentTypeId);

            // Act
            EmployeeDocumentData deactivated =
                await _fixture.DocumentRepository
                    .SetEmployeeDocumentStatusAsync(
                        employeeDocumentId:
                            document.EmployeeDocumentId,
                        isActive:
                            false,
                        expectedRowVersion:
                            document.RowVersion,
                        actorUserId:
                            _fixture
                                .SuperAdministratorUserId,
                        requestContext:
                            AuthenticationDatabaseFixture
                                .CreateRequestContext(
                                    "/integration-tests/" +
                                    "documents/deactivate"),
                        cancellationToken:
                            CancellationToken.None);

            // Assert
            Assert.False(deactivated.IsActive);
            Assert.NotNull(
                deactivated.DeactivatedAtUtc);
            Assert.Equal(
                _fixture.SuperAdministratorUserId,
                deactivated.DeactivatedByUserId);
            Assert.Equal(
                8,
                deactivated.RowVersion.Length);
            Assert.NotEqual(
                document.RowVersion,
                deactivated.RowVersion);
        }
        finally
        {
            await CleanupAsync(
                departmentCode,
                identificationNumber);
        }
    }

    private async Task<int> CreateEmployeeAsync(
        string departmentCode,
        string identificationNumber)
    {
        DepartmentData department =
            await _fixture.DepartmentRepository
                .CreateDepartmentAsync(
                    departmentCode:
                        departmentCode,
                    name:
                        "Document Tests",
                    description:
                        "Created by integration tests.",
                    actorUserId:
                        _fixture.SuperAdministratorUserId,
                    requestContext:
                        AuthenticationDatabaseFixture
                            .CreateRequestContext(
                                "/integration-tests/" +
                                "documents/create-department"),
                    cancellationToken:
                        CancellationToken.None);

        await _fixture.CreateActiveEmployeeForDepartmentAsync(
            department.DepartmentId,
            identificationNumber);

        return await _fixture
            .GetEmployeeIdByIdentificationNumberAsync(
                identificationNumber);
    }

    private async Task<DocumentTypeData>
        GetEmploymentContractTypeAsync()
    {
        IReadOnlyList<DocumentTypeData> documentTypes =
            await _fixture.DocumentRepository
                .GetDocumentTypesAsync(
                    actorUserId:
                        _fixture.SuperAdministratorUserId,
                    isActive:
                        true,
                    cancellationToken:
                        CancellationToken.None);

        return Assert.Single(
            documentTypes,
            documentType =>
                documentType.DocumentTypeCode
                    == "EmploymentContract");
    }

    private Task<EmployeeDocumentData> CreateDocumentAsync(
        int employeeId,
        int documentTypeId)
    {
        return _fixture.DocumentRepository
            .CreateEmployeeDocumentAsync(
                employeeId:
                    employeeId,
                documentTypeId:
                    documentTypeId,
                title:
                    "Employment contract",
                description:
                    "Created by integration tests.",
                originalFileName:
                    "contract.pdf",
                storageProvider:
                    "IntegrationTests",
                storageKey:
                    CreateStorageKey(),
                contentType:
                    "application/pdf",
                fileSizeBytes:
                    128,
                fileHash:
                    CreateFileHash(),
                issuedDate:
                    null,
                expirationDate:
                    null,
                isVisibleToEmployee:
                    true,
                actorUserId:
                    _fixture.SuperAdministratorUserId,
                requestContext:
                    AuthenticationDatabaseFixture
                        .CreateRequestContext(
                            "/integration-tests/" +
                            "documents/create-helper"),
                cancellationToken:
                    CancellationToken.None);
    }

    private async Task CleanupAsync(
        string departmentCode,
        string identificationNumber)
    {
        await _fixture.RemoveDocumentTestDataAsync(
            identificationNumber);

        await _fixture.RemoveDepartmentTestDataAsync(
            departmentCode,
            identificationNumber);
    }

    private static AuthenticationRequestContext
        CreateRequestContext(
            Guid correlationId,
            string requestPath)
    {
        return new AuthenticationRequestContext(
            CorrelationId:
                correlationId,
            ClientIpAddress:
                "127.0.0.1",
            UserAgent:
                "LithoManager.IntegrationTests",
            RequestPath:
                requestPath);
    }

    private static string CreateDepartmentCode()
    {
        return "DO" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }

    private static string CreateIdentificationNumber()
    {
        return "DOC-" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }

    private static string CreateStorageKey()
    {
        return "integration-tests/" +
            Guid.NewGuid().ToString("N");
    }

    private static byte[] CreateFileHash()
    {
        return Enumerable
            .Range(1, 32)
            .Select(value => (byte)value)
            .ToArray();
    }
}
