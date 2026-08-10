namespace LithoManager.Application.Features
    .HumanResources.Employees.SetEmployeeStatus;

public interface ISetEmployeeStatusService
{
    Task<EmployeeResult> SetAsync(
        SetEmployeeStatusCommand command,
        CancellationToken cancellationToken);
}
