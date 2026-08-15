namespace LithoManager.Application.Features
    .Documents.GetEmployeeDocuments;

public interface IGetEmployeeDocumentsService
{
    Task<EmployeeDocumentsResult> GetAsync(
        GetEmployeeDocumentsQuery query,
        CancellationToken cancellationToken);
}
