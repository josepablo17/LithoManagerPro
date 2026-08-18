namespace LithoManager.Application.Features
    .Payroll.GetAttendanceRecords;

public interface IGetAttendanceRecordsService
{
    Task<PayrollItemsResult<AttendanceRecordInfo>> GetAsync(
        GetAttendanceRecordsQuery query,
        CancellationToken cancellationToken);
}
