using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Documents.GetDocumentTypes;

public sealed class GetDocumentTypesService
    : IGetDocumentTypesService
{
    private readonly IDocumentRepository _documentRepository;

    public GetDocumentTypesService(
        IDocumentRepository documentRepository)
    {
        ArgumentNullException.ThrowIfNull(documentRepository);

        _documentRepository = documentRepository;
    }

    public async Task<DocumentTypesResult> GetAsync(
        GetDocumentTypesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!DocumentValidation.IsValidPositiveId(
                query.ActorUserId))
        {
            return DocumentTypesResult.Failure(
                DocumentErrorCode.InvalidRequest);
        }

        try
        {
            IReadOnlyList<DocumentTypeData> documentTypes =
                await _documentRepository
                    .GetDocumentTypesAsync(
                        actorUserId:
                            query.ActorUserId,
                        isActive:
                            query.IsActive,
                        cancellationToken:
                            cancellationToken);

            return DocumentTypesResult.Success(
                documentTypes
                    .Select(DocumentMapper.Map)
                    .ToList());
        }
        catch (DocumentPersistenceException exception)
        {
            return DocumentTypesResult.Failure(
                exception.ErrorCode);
        }
    }
}
