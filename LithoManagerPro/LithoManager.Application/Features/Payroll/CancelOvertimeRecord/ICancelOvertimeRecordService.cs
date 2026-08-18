namespace LithoManager.Application.Features
    .Payroll.CancelOvertimeRecord;

public interface ICancelOvertimeRecordService
{
    Task<OvertimeRecordResult> CancelAsync(
        CancelOvertimeRecordCommand command,
        CancellationToken cancellationToken);
}
