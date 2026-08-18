using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.GetEmployeeDisabilityById;

public sealed class GetEmployeeDisabilityByIdService
    : IGetEmployeeDisabilityByIdService
{
    private readonly IPayrollRepository _payrollRepository;

    public GetEmployeeDisabilityByIdService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<EmployeeDisabilityResult> GetAsync(
        GetEmployeeDisabilityByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!PayrollValidation.IsValidPositiveId(
                query.EmployeeDisabilityId)
            || !PayrollValidation.IsValidPositiveId(
                query.ActorUserId))
        {
            return EmployeeDisabilityResult.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeDisabilityData? disability =
                await _payrollRepository
                    .GetEmployeeDisabilityByIdAsync(
                        query.EmployeeDisabilityId,
                        query.ActorUserId,
                        cancellationToken);

            return disability is null
                ? EmployeeDisabilityResult.Failure(
                    PayrollErrorCode.EmployeeDisabilityNotFound)
                : EmployeeDisabilityResult.Success(
                    PayrollMapper.Map(disability));
        }
        catch (PayrollPersistenceException exception)
        {
            return EmployeeDisabilityResult.Failure(
                exception.ErrorCode);
        }
    }
}
