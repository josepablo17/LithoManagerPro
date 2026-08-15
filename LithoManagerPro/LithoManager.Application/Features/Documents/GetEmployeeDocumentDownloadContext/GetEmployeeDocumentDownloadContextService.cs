using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Documents.GetEmployeeDocumentDownloadContext;

public sealed class GetEmployeeDocumentDownloadContextService
    : IGetEmployeeDocumentDownloadContextService
{
    private readonly IDocumentRepository _documentRepository;

    public GetEmployeeDocumentDownloadContextService(
        IDocumentRepository documentRepository)
    {
        ArgumentNullException.ThrowIfNull(documentRepository);

        _documentRepository = documentRepository;
    }

    public async Task<EmployeeDocumentDownloadContextResult> GetAsync(
        GetEmployeeDocumentDownloadContextCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!DocumentValidation.IsValidPositiveId(
                command.EmployeeDocumentId)
            || !DocumentValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext))
        {
            return EmployeeDocumentDownloadContextResult
                .Failure(DocumentErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeDocumentDownloadContextData? downloadContext =
                await _documentRepository
                    .GetEmployeeDocumentDownloadContextAsync(
                        employeeDocumentId:
                            command.EmployeeDocumentId,
                        actorUserId:
                            command.ActorUserId,
                        requestContext:
                            command.RequestContext,
                        cancellationToken:
                            cancellationToken);

            if (downloadContext is null)
            {
                return EmployeeDocumentDownloadContextResult
                    .Failure(
                        DocumentErrorCode
                            .EmployeeDocumentNotFound);
            }

            return EmployeeDocumentDownloadContextResult
                .Success(DocumentMapper.Map(downloadContext));
        }
        catch (DocumentPersistenceException exception)
        {
            return EmployeeDocumentDownloadContextResult
                .Failure(exception.ErrorCode);
        }
    }
}
