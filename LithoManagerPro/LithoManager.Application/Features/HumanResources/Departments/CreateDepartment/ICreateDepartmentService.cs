namespace LithoManager.Application.Features
    .HumanResources.Departments.CreateDepartment;

public interface ICreateDepartmentService
{
    Task<DepartmentResult> CreateAsync(
        CreateDepartmentCommand command,
        CancellationToken cancellationToken);
}
