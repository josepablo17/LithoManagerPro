namespace LithoManager.Application.Features.LeaveManagement;

public sealed record EmployeeLeaveBalanceResult(
    bool IsSuccessful,
    LeaveManagementErrorCode ErrorCode,
    EmployeeLeaveBalanceInfo? LeaveBalance)
{
    public static EmployeeLeaveBalanceResult Success(
        EmployeeLeaveBalanceInfo leaveBalance)
    {
        ArgumentNullException.ThrowIfNull(leaveBalance);

        return new EmployeeLeaveBalanceResult(
            IsSuccessful: true,
            ErrorCode: LeaveManagementErrorCode.None,
            LeaveBalance: leaveBalance);
    }

    public static EmployeeLeaveBalanceResult Failure(
        LeaveManagementErrorCode errorCode)
    {
        if (errorCode == LeaveManagementErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new EmployeeLeaveBalanceResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            LeaveBalance: null);
    }
}
