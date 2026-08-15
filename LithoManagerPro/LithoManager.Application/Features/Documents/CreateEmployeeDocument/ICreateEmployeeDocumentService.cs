namespace LithoManager.Application.Features
    .Documents.CreateEmployeeDocument;

public interface ICreateEmployeeDocumentService
{
    Task<EmployeeDocumentResult> CreateAsync(
        CreateEmployeeDocumentCommand command,
        CancellationToken cancellationToken);
}
