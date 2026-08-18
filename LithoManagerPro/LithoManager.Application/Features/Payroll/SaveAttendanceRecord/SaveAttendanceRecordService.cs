using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.SaveAttendanceRecord;

public sealed class SaveAttendanceRecordService
    : ISaveAttendanceRecordService
{
    private readonly IPayrollRepository _payrollRepository;

    public SaveAttendanceRecordService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<AttendanceRecordResult> SaveAsync(
        SaveAttendanceRecordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!PayrollValidation.IsValidPositiveId(
                command.EmployeeId)
            || command.AttendanceDate is null
            || !PayrollValidation.IsValidAttendanceStatus(
                command.AttendanceStatus)
            || !PayrollValidation.IsValidHours(
                command.ExpectedHours)
            || !PayrollValidation.IsValidHours(
                command.WorkedHours)
            || !PayrollValidation.IsValidHours(
                command.PaidHours)
            || !PayrollValidation.IsValidHours(
                command.UnpaidHours)
            || !PayrollValidation.IsValidOptionalPositiveId(
                command.WorkShiftTypeId)
            || !PayrollValidation.IsValidNotes(
                command.Notes)
            || (command.ExpectedRowVersion is not null
                && !PayrollValidation.IsValidRowVersion(
                    command.ExpectedRowVersion))
            || !PayrollValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext))
        {
            return AttendanceRecordResult.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        try
        {
            AttendanceRecordData attendanceRecord =
                await _payrollRepository
                    .SaveAttendanceRecordAsync(
                        command.EmployeeId,
                        command.AttendanceDate.Value.Date,
                        PayrollValidation.NormalizeRequiredText(
                            command.AttendanceStatus!),
                        command.ExpectedHours!.Value,
                        command.WorkedHours!.Value,
                        command.PaidHours!.Value,
                        command.UnpaidHours!.Value,
                        command.WorkShiftTypeId,
                        command.IsPaidHoliday,
                        command.IsApproved,
                        PayrollValidation.NormalizeOptionalText(
                            command.Notes),
                        command.ExpectedRowVersion,
                        command.ActorUserId,
                        command.RequestContext,
                        cancellationToken);

            return AttendanceRecordResult.Success(
                PayrollMapper.Map(attendanceRecord));
        }
        catch (PayrollPersistenceException exception)
        {
            return AttendanceRecordResult.Failure(
                exception.ErrorCode);
        }
    }
}
