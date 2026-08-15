namespace LithoManager.Application.Features
    .Documents.GetEmployeeDocumentDownloadContext;

public interface IGetEmployeeDocumentDownloadContextService
{
    Task<EmployeeDocumentDownloadContextResult> GetAsync(
        GetEmployeeDocumentDownloadContextCommand command,
        CancellationToken cancellationToken);
}
