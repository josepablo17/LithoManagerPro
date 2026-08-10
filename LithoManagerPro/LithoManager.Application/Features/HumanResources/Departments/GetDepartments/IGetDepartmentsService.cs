namespace LithoManager.Application.Features
    .HumanResources.Departments.GetDepartments;

public interface IGetDepartmentsService
{
    Task<DepartmentsResult> GetAsync(
        GetDepartmentsQuery query,
        CancellationToken cancellationToken);
}
