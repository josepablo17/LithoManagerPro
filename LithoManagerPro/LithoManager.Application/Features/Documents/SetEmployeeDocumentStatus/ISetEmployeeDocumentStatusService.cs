namespace LithoManager.Application.Features
    .Documents.SetEmployeeDocumentStatus;

public interface ISetEmployeeDocumentStatusService
{
    Task<EmployeeDocumentResult> SetAsync(
        SetEmployeeDocumentStatusCommand command,
        CancellationToken cancellationToken);
}
