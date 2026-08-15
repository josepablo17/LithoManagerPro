using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.Documents;

namespace LithoManager.Application.Abstractions.Persistence;

public interface IDocumentRepository
{
    Task<IReadOnlyList<DocumentTypeData>> GetDocumentTypesAsync(
        int actorUserId,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<EmployeeRecordData> EnsureEmployeeRecordAsync(
        int employeeId,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeDocumentData>>
        GetEmployeeDocumentsAsync(
            int actorUserId,
            int? employeeId,
            int? documentTypeId,
            bool? isActive,
            bool? isVisibleToEmployee,
            DateTime? createdFromUtc,
            DateTime? createdToUtc,
            string? searchTerm,
            CancellationToken cancellationToken);

    Task<EmployeeDocumentData?> GetEmployeeDocumentByIdAsync(
        int employeeDocumentId,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<EmployeeDocumentDownloadContextData?>
        GetEmployeeDocumentDownloadContextAsync(
            int employeeDocumentId,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken);

    Task<EmployeeDocumentData> CreateEmployeeDocumentAsync(
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
        CancellationToken cancellationToken);

    Task<EmployeeDocumentData> UpdateEmployeeDocumentAsync(
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
        CancellationToken cancellationToken);

    Task<EmployeeDocumentData> SetEmployeeDocumentStatusAsync(
        int employeeDocumentId,
        bool isActive,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);
}
