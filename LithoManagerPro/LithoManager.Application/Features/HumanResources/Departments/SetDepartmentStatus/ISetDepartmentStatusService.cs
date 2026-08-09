namespace LithoManager.Application.Features
    .HumanResources.Departments.SetDepartmentStatus;

public interface ISetDepartmentStatusService
{
    Task<DepartmentResult> SetAsync(
        SetDepartmentStatusCommand command,
        CancellationToken cancellationToken);
}
