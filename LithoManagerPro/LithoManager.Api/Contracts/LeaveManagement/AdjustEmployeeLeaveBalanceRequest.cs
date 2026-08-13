namespace LithoManager.Api.Contracts.LeaveManagement;

public sealed class AdjustEmployeeLeaveBalanceRequest
{
    public string? LeaveTypeCode { get; init; }

    public decimal AdjustedDaysDelta { get; init; }
}
