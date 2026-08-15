using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Documents.GetEmployeeDocuments;

public sealed class GetEmployeeDocumentsService
    : IGetEmployeeDocumentsService
{
    private readonly IDocumentRepository _documentRepository;

    public GetEmployeeDocumentsService(
        IDocumentRepository documentRepository)
    {
        ArgumentNullException.ThrowIfNull(documentRepository);

        _documentRepository = documentRepository;
    }

    public async Task<EmployeeDocumentsResult> GetAsync(
        GetEmployeeDocumentsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        string? searchTerm =
            DocumentValidation.NormalizeOptionalText(
                query.SearchTerm);

        if (!DocumentValidation.IsValidPositiveId(
                query.ActorUserId)
            || !DocumentValidation.IsValidOptionalPositiveId(
                query.EmployeeId)
            || !DocumentValidation.IsValidOptionalPositiveId(
                query.DocumentTypeId)
            || !DocumentValidation.IsValidDateRange(
                query.CreatedFromUtc,
                query.CreatedToUtc)
            || !DocumentValidation.IsValidSearchTerm(
                searchTerm))
        {
            return EmployeeDocumentsResult.Failure(
                DocumentErrorCode.InvalidRequest);
        }

        try
        {
            IReadOnlyList<EmployeeDocumentData> documents =
                await _documentRepository
                    .GetEmployeeDocumentsAsync(
                        actorUserId:
                            query.ActorUserId,
                        employeeId:
                            query.EmployeeId,
                        documentTypeId:
                            query.DocumentTypeId,
                        isActive:
                            query.IsActive,
                        isVisibleToEmployee:
                            query.IsVisibleToEmployee,
                        createdFromUtc:
                            query.CreatedFromUtc,
                        createdToUtc:
                            query.CreatedToUtc,
                        searchTerm:
                            searchTerm,
                        cancellationToken:
                            cancellationToken);

            return EmployeeDocumentsResult.Success(
                documents
                    .Select(DocumentMapper.Map)
                    .ToList());
        }
        catch (DocumentPersistenceException exception)
        {
            return EmployeeDocumentsResult.Failure(
                exception.ErrorCode);
        }
    }
}
