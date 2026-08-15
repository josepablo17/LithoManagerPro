using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Documents.UpdateEmployeeDocument;

public sealed class UpdateEmployeeDocumentService
    : IUpdateEmployeeDocumentService
{
    private readonly IDocumentRepository _documentRepository;

    public UpdateEmployeeDocumentService(
        IDocumentRepository documentRepository)
    {
        ArgumentNullException.ThrowIfNull(documentRepository);

        _documentRepository = documentRepository;
    }

    public async Task<EmployeeDocumentResult> UpdateAsync(
        UpdateEmployeeDocumentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!DocumentValidation.IsValidPositiveId(
                command.EmployeeDocumentId)
            || !DocumentValidation.IsValidPositiveId(
                command.DocumentTypeId)
            || !DocumentValidation.IsValidRowVersion(
                command.ExpectedRowVersion)
            || !DocumentValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext)
            || !DocumentValidation.IsValidUpdateRequest(
                command.Title,
                command.Description,
                command.IssuedDate,
                command.ExpirationDate))
        {
            return EmployeeDocumentResult.Failure(
                DocumentErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeDocumentData employeeDocument =
                await _documentRepository
                    .UpdateEmployeeDocumentAsync(
                        employeeDocumentId:
                            command.EmployeeDocumentId,
                        documentTypeId:
                            command.DocumentTypeId,
                        title:
                            DocumentValidation
                                .NormalizeRequiredText(
                                    command.Title),
                        description:
                            DocumentValidation
                                .NormalizeOptionalText(
                                    command.Description),
                        issuedDate:
                            command.IssuedDate,
                        expirationDate:
                            command.ExpirationDate,
                        isVisibleToEmployee:
                            command.IsVisibleToEmployee,
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
