namespace LithoManager.Application.Features
    .Payroll.GetOvertimeRecords;

public interface IGetOvertimeRecordsService
{
    Task<PayrollItemsResult<OvertimeRecordInfo>> GetAsync(
        GetOvertimeRecordsQuery query,
        CancellationToken cancellationToken);
}
