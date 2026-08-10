namespace LithoManager.Application.Features
    .HumanResources.Employees.GetEmployees;

public interface IGetEmployeesService
{
    Task<EmployeesResult> GetAsync(
        GetEmployeesQuery query,
        CancellationToken cancellationToken);
}
