using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeById;

public sealed class GetEmployeeByIdService
    : IGetEmployeeByIdService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    public GetEmployeeByIdService(
        IEmployeeRepository employeeRepository)
    {
        ArgumentNullException.ThrowIfNull(
            employeeRepository);

        _employeeRepository =
            employeeRepository;
    }

    public async Task<EmployeeResult> GetAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        if (employeeId <= 0)
        {
            return EmployeeResult.Failure(
                EmployeeErrorCode.InvalidRequest);
        }

        EmployeeData? employee =
            await _employeeRepository.GetEmployeeByIdAsync(
                employeeId,
                cancellationToken);

        if (employee is null)
        {
            return EmployeeResult.Failure(
                EmployeeErrorCode.EmployeeNotFound);
        }

        return EmployeeResult.Success(
            EmployeeMapper.Map(employee));
    }
}
