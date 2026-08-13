namespace LithoManager.Application.Features
    .LeaveManagement.GetEmployeeLeaveBalance;

public interface IGetEmployeeLeaveBalanceService
{
    Task<EmployeeLeaveBalanceResult> GetAsync(
        GetEmployeeLeaveBalanceQuery query,
        CancellationToken cancellationToken);
}
