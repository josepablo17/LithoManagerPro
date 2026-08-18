namespace LithoManager.Application.Features
    .Payroll.ApproveEmployeeDisability;

public interface IApproveEmployeeDisabilityService
{
    Task<EmployeeDisabilityResult> ApproveAsync(
        ApproveEmployeeDisabilityCommand command,
        CancellationToken cancellationToken);
}
