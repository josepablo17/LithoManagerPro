namespace LithoManager.Application.Features
    .HumanResources.Employees.GetAssignableEmployeeUsers;

public interface IGetAssignableEmployeeUsersService
{
    Task<AssignableEmployeeUsersResult> GetAsync(
        GetAssignableEmployeeUsersQuery query,
        CancellationToken cancellationToken);
}
