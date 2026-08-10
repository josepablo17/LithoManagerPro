namespace LithoManager.Application.Features
    .HumanResources.Employees.UpdateEmployee;

public interface IUpdateEmployeeService
{
    Task<EmployeeResult> UpdateAsync(
        UpdateEmployeeCommand command,
        CancellationToken cancellationToken);
}
