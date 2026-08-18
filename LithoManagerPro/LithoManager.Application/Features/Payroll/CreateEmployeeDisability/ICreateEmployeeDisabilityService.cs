namespace LithoManager.Application.Features
    .Payroll.CreateEmployeeDisability;

public interface ICreateEmployeeDisabilityService
{
    Task<EmployeeDisabilityResult> CreateAsync(
        CreateEmployeeDisabilityCommand command,
        CancellationToken cancellationToken);
}
