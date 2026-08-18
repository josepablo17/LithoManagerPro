namespace LithoManager.Application.Features
    .Payroll.CreateOvertimeRecord;

public interface ICreateOvertimeRecordService
{
    Task<OvertimeRecordResult> CreateAsync(
        CreateOvertimeRecordCommand command,
        CancellationToken cancellationToken);
}
