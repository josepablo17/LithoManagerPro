namespace LithoManager.Application.Features
    .Documents.GetDocumentTypes;

public interface IGetDocumentTypesService
{
    Task<DocumentTypesResult> GetAsync(
        GetDocumentTypesQuery query,
        CancellationToken cancellationToken);
}
