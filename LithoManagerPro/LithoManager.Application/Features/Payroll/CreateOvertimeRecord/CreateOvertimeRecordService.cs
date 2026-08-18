using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.CreateOvertimeRecord;

public sealed class CreateOvertimeRecordService
    : ICreateOvertimeRecordService
{
    private readonly IPayrollRepository _payrollRepository;

    public CreateOvertimeRecordService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<OvertimeRecordResult> CreateAsync(
        CreateOvertimeRecordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!PayrollValidation.IsValidPositiveId(
                command.EmployeeId)
            || !PayrollValidation.IsValidPositiveId(
                command.OvertimeRuleId)
            || command.OvertimeDate is null
            || !PayrollValidation.IsValidPositiveHours(
                command.Hours)
            || !PayrollValidation.IsValidOptionalPositiveId(
                command.AttendanceRecordId)
            || !PayrollValidation.IsValidNotes(
                command.Notes)
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
                    .CreateOvertimeRecordAsync(
                        command.EmployeeId,
                        command.OvertimeRuleId,
                        command.OvertimeDate.Value.Date,
                        command.Hours!.Value,
                        command.AttendanceRecordId,
                        PayrollValidation.NormalizeOptionalText(
                            command.Notes),
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
