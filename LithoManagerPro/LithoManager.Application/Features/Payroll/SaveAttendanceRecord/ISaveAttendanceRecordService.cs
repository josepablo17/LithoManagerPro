namespace LithoManager.Application.Features
    .Payroll.SaveAttendanceRecord;

public interface ISaveAttendanceRecordService
{
    Task<AttendanceRecordResult> SaveAsync(
        SaveAttendanceRecordCommand command,
        CancellationToken cancellationToken);
}
