using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.RespondOvertimeRecord;

public sealed class RespondOvertimeRecordService
    : IRespondOvertimeRecordService
{
    private readonly IPayrollRepository _payrollRepository;

    public RespondOvertimeRecordService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<OvertimeRecordResult> RespondAsync(
        RespondOvertimeRecordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        bool isRejectionReasonValid =
            command.IsApproved
                ? PayrollValidation.IsValidOptionalReason(
                    command.RejectionReason)
                : PayrollValidation.IsValidReason(
                    command.RejectionReason);

        if (!PayrollValidation.IsValidPositiveId(
                command.OvertimeRecordId)
            || !isRejectionReasonValid
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
                    .RespondOvertimeRecordAsync(
                        command.OvertimeRecordId,
                        command.IsApproved,
                        PayrollValidation.NormalizeOptionalText(
                            command.RejectionReason),
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
