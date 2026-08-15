namespace LithoManager.Application.Features
    .Documents.GetEmployeeDocumentById;

public interface IGetEmployeeDocumentByIdService
{
    Task<EmployeeDocumentResult> GetAsync(
        int employeeDocumentId,
        int actorUserId,
        CancellationToken cancellationToken);
}
