using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Documents.EnsureEmployeeRecord;

public sealed class EnsureEmployeeRecordService
    : IEnsureEmployeeRecordService
{
    private readonly IDocumentRepository _documentRepository;

    public EnsureEmployeeRecordService(
        IDocumentRepository documentRepository)
    {
        ArgumentNullException.ThrowIfNull(documentRepository);

        _documentRepository = documentRepository;
    }

    public async Task<EmployeeRecordResult> EnsureAsync(
        EnsureEmployeeRecordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!DocumentValidation.IsValidPositiveId(
                command.EmployeeId)
            || !DocumentValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext))
        {
            return EmployeeRecordResult.Failure(
                DocumentErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeRecordData employeeRecord =
                await _documentRepository
                    .EnsureEmployeeRecordAsync(
                        employeeId:
                            command.EmployeeId,
                        actorUserId:
                            command.ActorUserId,
                        requestContext:
                            command.RequestContext,
                        cancellationToken:
                            cancellationToken);

            return EmployeeRecordResult.Success(
                DocumentMapper.Map(employeeRecord));
        }
        catch (DocumentPersistenceException exception)
        {
            return EmployeeRecordResult.Failure(
                exception.ErrorCode);
        }
    }
}
