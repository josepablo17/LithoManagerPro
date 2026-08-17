namespace LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeIdentificationTypes;

public interface IGetEmployeeIdentificationTypesService
{
    Task<EmployeeIdentificationTypesResult> GetAsync(
        GetEmployeeIdentificationTypesQuery query,
        CancellationToken cancellationToken);
}
