namespace LithoManager.Application.Features
    .Documents.UpdateEmployeeDocument;

public interface IUpdateEmployeeDocumentService
{
    Task<EmployeeDocumentResult> UpdateAsync(
        UpdateEmployeeDocumentCommand command,
        CancellationToken cancellationToken);
}
