using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Documents.GetEmployeeDocumentById;

public sealed class GetEmployeeDocumentByIdService
    : IGetEmployeeDocumentByIdService
{
    private readonly IDocumentRepository _documentRepository;

    public GetEmployeeDocumentByIdService(
        IDocumentRepository documentRepository)
    {
        ArgumentNullException.ThrowIfNull(documentRepository);

        _documentRepository = documentRepository;
    }

    public async Task<EmployeeDocumentResult> GetAsync(
        int employeeDocumentId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (!DocumentValidation.IsValidPositiveId(
                employeeDocumentId)
            || !DocumentValidation.IsValidPositiveId(
                actorUserId))
        {
            return EmployeeDocumentResult.Failure(
                DocumentErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeDocumentData? employeeDocument =
                await _documentRepository
                    .GetEmployeeDocumentByIdAsync(
                        employeeDocumentId,
                        actorUserId,
                        cancellationToken);

            if (employeeDocument is null)
            {
                return EmployeeDocumentResult.Failure(
                    DocumentErrorCode.EmployeeDocumentNotFound);
            }

            return EmployeeDocumentResult.Success(
                DocumentMapper.Map(employeeDocument));
        }
        catch (DocumentPersistenceException exception)
        {
            return EmployeeDocumentResult.Failure(
                exception.ErrorCode);
        }
    }
}
