using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Documents.SetEmployeeDocumentStatus;

public sealed class SetEmployeeDocumentStatusService
    : ISetEmployeeDocumentStatusService
{
    private readonly IDocumentRepository _documentRepository;

    public SetEmployeeDocumentStatusService(
        IDocumentRepository documentRepository)
    {
        ArgumentNullException.ThrowIfNull(documentRepository);

        _documentRepository = documentRepository;
    }

    public async Task<EmployeeDocumentResult> SetAsync(
        SetEmployeeDocumentStatusCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!DocumentValidation.IsValidPositiveId(
                command.EmployeeDocumentId)
            || !DocumentValidation.IsValidRowVersion(
                command.ExpectedRowVersion)
            || !DocumentValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext))
        {
            return EmployeeDocumentResult.Failure(
                DocumentErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeDocumentData employeeDocument =
                await _documentRepository
                    .SetEmployeeDocumentStatusAsync(
                        employeeDocumentId:
                            command.EmployeeDocumentId,
                        isActive:
                            command.IsActive,
                        expectedRowVersion:
                            command.ExpectedRowVersion,
                        actorUserId:
                            command.ActorUserId,
                        requestContext:
                            command.RequestContext,
                        cancellationToken:
                            cancellationToken);

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
