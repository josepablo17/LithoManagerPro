namespace LithoManager.Application.Features
    .Payroll.RespondOvertimeRecord;

public interface IRespondOvertimeRecordService
{
    Task<OvertimeRecordResult> RespondAsync(
        RespondOvertimeRecordCommand command,
        CancellationToken cancellationToken);
}
