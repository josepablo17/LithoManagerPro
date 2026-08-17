using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Employees.GetAssignableEmployeeUsers;

public sealed class GetAssignableEmployeeUsersService
    : IGetAssignableEmployeeUsersService
{
    private readonly IEmployeeRepository _employeeRepository;

    public GetAssignableEmployeeUsersService(
        IEmployeeRepository employeeRepository)
    {
        ArgumentNullException.ThrowIfNull(
            employeeRepository);

        _employeeRepository = employeeRepository;
    }

    public async Task<AssignableEmployeeUsersResult> GetAsync(
        GetAssignableEmployeeUsersQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.EmployeeId is <= 0)
        {
            return AssignableEmployeeUsersResult.Failure(
                EmployeeErrorCode.InvalidRequest);
        }

        try
        {
            IReadOnlyList<AssignableEmployeeUserData> users =
                await _employeeRepository
                    .GetAssignableEmployeeUsersAsync(
                        query.EmployeeId,
                        cancellationToken);

            return AssignableEmployeeUsersResult.Success(
                users
                    .Select(EmployeeMapper.Map)
                    .ToList());
        }
        catch (EmployeePersistenceException exception)
        {
            return AssignableEmployeeUsersResult.Failure(
                exception.ErrorCode);
        }
    }
}
