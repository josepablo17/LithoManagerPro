using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.ApproveEmployeeDisability;

public sealed class ApproveEmployeeDisabilityService
    : IApproveEmployeeDisabilityService
{
    private readonly IPayrollRepository _payrollRepository;

    public ApproveEmployeeDisabilityService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<EmployeeDisabilityResult> ApproveAsync(
        ApproveEmployeeDisabilityCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!PayrollValidation.IsValidPositiveId(
                command.EmployeeDisabilityId)
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
                    .ApproveEmployeeDisabilityAsync(
                        command.EmployeeDisabilityId,
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
