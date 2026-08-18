using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.CancelOvertimeRecord;

public sealed class CancelOvertimeRecordService
    : ICancelOvertimeRecordService
{
    private readonly IPayrollRepository _payrollRepository;

    public CancelOvertimeRecordService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<OvertimeRecordResult> CancelAsync(
        CancelOvertimeRecordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!PayrollValidation.IsValidPositiveId(
                command.OvertimeRecordId)
            || !PayrollValidation.IsValidRowVersion(
                command.ExpectedRowVersion)
            || !PayrollValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext))
        {
            return OvertimeRecordResult.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        try
        {
            OvertimeRecordData overtimeRecord =
                await _payrollRepository
                    .CancelOvertimeRecordAsync(
                        command.OvertimeRecordId,
                        command.ExpectedRowVersion!,
                        command.ActorUserId,
                        command.RequestContext,
                        cancellationToken);

            return OvertimeRecordResult.Success(
                PayrollMapper.Map(overtimeRecord));
        }
        catch (PayrollPersistenceException exception)
        {
            return OvertimeRecordResult.Failure(
                exception.ErrorCode);
        }
    }
}
