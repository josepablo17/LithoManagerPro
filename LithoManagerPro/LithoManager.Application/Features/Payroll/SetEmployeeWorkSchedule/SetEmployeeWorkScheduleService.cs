using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.SetEmployeeWorkSchedule;

public sealed class SetEmployeeWorkScheduleService
    : ISetEmployeeWorkScheduleService
{
    private readonly IPayrollRepository _payrollRepository;

    public SetEmployeeWorkScheduleService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<EmployeeWorkScheduleResult> SetAsync(
        SetEmployeeWorkScheduleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!PayrollValidation.IsValidPositiveId(
                command.EmployeeId)
            || !PayrollValidation.IsValidPositiveId(
                command.WorkShiftTypeId)
            || !PayrollValidation.IsValidEffectiveDate(
                command.EffectiveFromDate)
            || !PayrollValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext))
        {
            return EmployeeWorkScheduleResult.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeWorkScheduleData schedule =
                await _payrollRepository
                    .SetEmployeeWorkScheduleAsync(
                        command.EmployeeId,
                        command.WorkShiftTypeId,
                        command.EffectiveFromDate!.Value.Date,
                        command.ActorUserId,
                        command.RequestContext,
                        cancellationToken);

            return EmployeeWorkScheduleResult.Success(
                PayrollMapper.Map(schedule));
        }
        catch (PayrollPersistenceException exception)
        {
            return EmployeeWorkScheduleResult.Failure(
                exception.ErrorCode);
        }
    }
}
