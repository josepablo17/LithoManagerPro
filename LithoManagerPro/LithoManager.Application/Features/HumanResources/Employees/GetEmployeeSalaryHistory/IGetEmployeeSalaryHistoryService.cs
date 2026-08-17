namespace LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeSalaryHistory;

public interface IGetEmployeeSalaryHistoryService
{
    Task<EmployeeSalaryHistoryResult> GetAsync(
        GetEmployeeSalaryHistoryQuery query,
        CancellationToken cancellationToken);
}
