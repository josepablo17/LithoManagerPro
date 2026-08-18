namespace LithoManager.Application.Features
    .Payroll.CancelEmployeeDisability;

public interface ICancelEmployeeDisabilityService
{
    Task<EmployeeDisabilityResult> CancelAsync(
        CancelEmployeeDisabilityCommand command,
        CancellationToken cancellationToken);
}
