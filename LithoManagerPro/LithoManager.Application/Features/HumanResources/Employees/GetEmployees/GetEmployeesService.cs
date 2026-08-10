using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Employees.GetEmployees;

public sealed class GetEmployeesService
    : IGetEmployeesService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    public GetEmployeesService(
        IEmployeeRepository employeeRepository)
    {
        ArgumentNullException.ThrowIfNull(
            employeeRepository);

        _employeeRepository =
            employeeRepository;
    }

    public async Task<EmployeesResult> GetAsync(
        GetEmployeesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!EmployeeValidation.IsValidSearchTerm(
                query.SearchTerm)
            || !EmployeeValidation.IsValidDepartmentFilter(
                query.DepartmentId))
        {
            return EmployeesResult.Failure(
                EmployeeErrorCode.InvalidRequest);
        }

        IReadOnlyList<EmployeeData> employees =
            await _employeeRepository.GetEmployeesAsync(
                query.SearchTerm,
                query.DepartmentId,
                query.IsActive,
                cancellationToken);

        return EmployeesResult.Success(
            employees
                .Select(EmployeeMapper.Map)
                .ToList());
    }
}
