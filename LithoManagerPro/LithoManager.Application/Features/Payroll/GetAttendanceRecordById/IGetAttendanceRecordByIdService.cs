namespace LithoManager.Application.Features
    .Payroll.GetAttendanceRecordById;

public interface IGetAttendanceRecordByIdService
{
    Task<AttendanceRecordResult> GetAsync(
        GetAttendanceRecordByIdQuery query,
        CancellationToken cancellationToken);
}
