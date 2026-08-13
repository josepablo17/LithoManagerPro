namespace LithoManager.Application.Features
    .LeaveManagement.AdjustEmployeeLeaveBalance;

public interface IAdjustEmployeeLeaveBalanceService
{
    Task<EmployeeLeaveBalanceResult> AdjustAsync(
        AdjustEmployeeLeaveBalanceCommand command,
        CancellationToken cancellationToken);
}
