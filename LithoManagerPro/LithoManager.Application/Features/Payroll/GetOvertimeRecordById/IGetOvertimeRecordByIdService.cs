namespace LithoManager.Application.Features
    .Payroll.GetOvertimeRecordById;

public interface IGetOvertimeRecordByIdService
{
    Task<OvertimeRecordResult> GetAsync(
        GetOvertimeRecordByIdQuery query,
        CancellationToken cancellationToken);
}
