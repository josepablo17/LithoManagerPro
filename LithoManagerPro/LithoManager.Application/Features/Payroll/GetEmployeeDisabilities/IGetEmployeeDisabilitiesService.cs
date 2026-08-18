namespace LithoManager.Application.Features
    .Payroll.GetEmployeeDisabilities;

public interface IGetEmployeeDisabilitiesService
{
    Task<PayrollItemsResult<EmployeeDisabilityInfo>> GetAsync(
        GetEmployeeDisabilitiesQuery query,
        CancellationToken cancellationToken);
}
