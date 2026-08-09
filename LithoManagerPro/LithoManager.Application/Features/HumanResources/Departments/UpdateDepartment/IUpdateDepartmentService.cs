namespace LithoManager.Application.Features
    .HumanResources.Departments.UpdateDepartment;

public interface IUpdateDepartmentService
{
    Task<DepartmentResult> UpdateAsync(
        UpdateDepartmentCommand command,
        CancellationToken cancellationToken);
}
