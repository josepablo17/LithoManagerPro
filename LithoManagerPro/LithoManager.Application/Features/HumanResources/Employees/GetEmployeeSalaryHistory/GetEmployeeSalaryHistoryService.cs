using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeSalaryHistory;

public sealed class GetEmployeeSalaryHistoryService
    : IGetEmployeeSalaryHistoryService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    public GetEmployeeSalaryHistoryService(
        IEmployeeRepository employeeRepository)
    {
        ArgumentNullException.ThrowIfNull(
            employeeRepository);

        _employeeRepository =
            employeeRepository;
    }

    public async Task<EmployeeSalaryHistoryResult> GetAsync(
        GetEmployeeSalaryHistoryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!EmployeeValidation.IsValidActorUserId(
                query.ActorUserId)
            || !EmployeeValidation.IsValidEmployeeId(
                query.EmployeeId)
            || !EmployeeValidation.IsValidEffectiveDateRange(
                query.EffectiveFromDate,
                query.EffectiveToDate))
        {
            return EmployeeSalaryHistoryResult.Failure(
                EmployeeErrorCode.InvalidRequest);
        }

        try
        {
            IReadOnlyList<EmployeeSalaryHistoryData>
                salaryHistory =
                    await _employeeRepository
                        .GetEmployeeSalaryHistoryAsync(
                            actorUserId:
                                query.ActorUserId,
                            employeeId:
                                query.EmployeeId,
                            effectiveFromDate:
                                query.EffectiveFromDate,
                            effectiveToDate:
                                query.EffectiveToDate,
                            cancellationToken:
                                cancellationToken);

            return EmployeeSalaryHistoryResult.Success(
                salaryHistory
                    .Select(EmployeeMapper.Map)
                    .ToList());
        }
        catch (EmployeePersistenceException exception)
        {
            return EmployeeSalaryHistoryResult.Failure(
                exception.ErrorCode);
        }
    }
}
