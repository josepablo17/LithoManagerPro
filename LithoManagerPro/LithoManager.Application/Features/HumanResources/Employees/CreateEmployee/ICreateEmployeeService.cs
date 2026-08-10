namespace LithoManager.Application.Features
    .HumanResources.Employees.CreateEmployee;

public interface ICreateEmployeeService
{
    Task<EmployeeResult> CreateAsync(
        CreateEmployeeCommand command,
        CancellationToken cancellationToken);
}
