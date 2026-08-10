namespace LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeById;

public interface IGetEmployeeByIdService
{
    Task<EmployeeResult> GetAsync(
        int employeeId,
        CancellationToken cancellationToken);
}
