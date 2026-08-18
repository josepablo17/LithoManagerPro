namespace LithoManager.Application.Features
    .Payroll.SetEmployeeWorkSchedule;

public interface ISetEmployeeWorkScheduleService
{
    Task<EmployeeWorkScheduleResult> SetAsync(
        SetEmployeeWorkScheduleCommand command,
        CancellationToken cancellationToken);
}
