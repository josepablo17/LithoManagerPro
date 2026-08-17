using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeIdentificationTypes;

public sealed class GetEmployeeIdentificationTypesService
    : IGetEmployeeIdentificationTypesService
{
    private readonly IEmployeeRepository _employeeRepository;

    public GetEmployeeIdentificationTypesService(
        IEmployeeRepository employeeRepository)
    {
        ArgumentNullException.ThrowIfNull(
            employeeRepository);

        _employeeRepository = employeeRepository;
    }

    public async Task<EmployeeIdentificationTypesResult> GetAsync(
        GetEmployeeIdentificationTypesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            IReadOnlyList<EmployeeIdentificationTypeData>
                identificationTypes =
                    await _employeeRepository
                        .GetEmployeeIdentificationTypesAsync(
                            cancellationToken);

            return EmployeeIdentificationTypesResult.Success(
                identificationTypes
                    .Select(EmployeeMapper.Map)
                    .ToList());
        }
        catch (EmployeePersistenceException exception)
        {
            return EmployeeIdentificationTypesResult.Failure(
                exception.ErrorCode);
        }
    }
}
