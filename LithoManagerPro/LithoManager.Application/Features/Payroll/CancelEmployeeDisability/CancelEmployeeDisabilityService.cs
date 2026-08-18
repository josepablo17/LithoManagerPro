using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.CancelEmployeeDisability;

public sealed class CancelEmployeeDisabilityService
    : ICancelEmployeeDisabilityService
{
    private readonly IPayrollRepository _payrollRepository;

    public CancelEmployeeDisabilityService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<EmployeeDisabilityResult> CancelAsync(
        CancelEmployeeDisabilityCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!PayrollValidation.IsValidPositiveId(
                command.EmployeeDisabilityId)
            || !PayrollValidation.IsValidReason(
                command.CancellationReason)
            || !PayrollValidation.IsValidRowVersion(
                command.ExpectedRowVersion)
            || !PayrollValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext))
        {
            return EmployeeDisabilityResult.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeDisabilityData employeeDisability =
                await _payrollRepository
                    .CancelEmployeeDisabilityAsync(
                        command.EmployeeDisabilityId,
                        PayrollValidation.NormalizeRequiredText(
                            command.CancellationReason!),
                        command.ExpectedRowVersion!,
                        command.ActorUserId,
                        command.RequestContext,
                        cancellationToken);

            return EmployeeDisabilityResult.Success(
                PayrollMapper.Map(employeeDisability));
        }
        catch (PayrollPersistenceException exception)
        {
            return EmployeeDisabilityResult.Failure(
                exception.ErrorCode);
        }
    }
}
