namespace LithoManager.Application.Features
    .HumanResources.Departments.GetDepartmentById;

public interface IGetDepartmentByIdService
{
    Task<DepartmentResult> GetAsync(
        int departmentId,
        CancellationToken cancellationToken);
}
