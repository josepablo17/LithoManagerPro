namespace LithoManager.Application.Features
    .Payroll.GetEmployeeDisabilityById;

public interface IGetEmployeeDisabilityByIdService
{
    Task<EmployeeDisabilityResult> GetAsync(
        GetEmployeeDisabilityByIdQuery query,
        CancellationToken cancellationToken);
}
