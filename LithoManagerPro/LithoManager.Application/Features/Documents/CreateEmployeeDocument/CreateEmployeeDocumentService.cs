using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Documents.CreateEmployeeDocument;

public sealed class CreateEmployeeDocumentService
    : ICreateEmployeeDocumentService
{
    private readonly IDocumentRepository _documentRepository;

    public CreateEmployeeDocumentService(
        IDocumentRepository documentRepository)
    {
        ArgumentNullException.ThrowIfNull(documentRepository);

        _documentRepository = documentRepository;
    }

    public async Task<EmployeeDocumentResult> CreateAsync(
        CreateEmployeeDocumentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!DocumentValidation.IsValidPositiveId(
                command.EmployeeId)
            || !DocumentValidation.IsValidPositiveId(
                command.DocumentTypeId)
            || !DocumentValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext)
            || !DocumentValidation.IsValidCreateRequest(
                command.Title,
                command.Description,
                command.OriginalFileName,
                command.StorageProvider,
                command.StorageKey,
                command.ContentType,
                command.FileSizeBytes,
                command.FileHash,
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
                    .CreateEmployeeDocumentAsync(
                        employeeId:
                            command.EmployeeId,
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
                        originalFileName:
                            DocumentValidation
                                .NormalizeRequiredText(
                                    command.OriginalFileName),
                        storageProvider:
                            DocumentValidation
                                .NormalizeRequiredText(
                                    command.StorageProvider),
                        storageKey:
                            DocumentValidation
                                .NormalizeRequiredText(
                                    command.StorageKey),
                        contentType:
                            DocumentValidation
                                .NormalizeRequiredText(
                                    command.ContentType),
                        fileSizeBytes:
                            command.FileSizeBytes,
                        fileHash:
                            command.FileHash,
                        issuedDate:
                            command.IssuedDate,
                        expirationDate:
                            command.ExpirationDate,
                        isVisibleToEmployee:
                            command.IsVisibleToEmployee,
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
