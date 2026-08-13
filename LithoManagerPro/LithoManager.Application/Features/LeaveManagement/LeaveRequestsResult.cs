namespace LithoManager.Application.Features.LeaveManagement;

public sealed record LeaveRequestsResult(
    bool IsSuccessful,
    LeaveManagementErrorCode ErrorCode,
    IReadOnlyList<LeaveRequestInfo> LeaveRequests)
{
    public static LeaveRequestsResult Success(
        IReadOnlyList<LeaveRequestInfo> leaveRequests)
    {
        ArgumentNullException.ThrowIfNull(leaveRequests);

        return new LeaveRequestsResult(
            IsSuccessful: true,
            ErrorCode: LeaveManagementErrorCode.None,
            LeaveRequests: leaveRequests);
    }

    public static LeaveRequestsResult Failure(
        LeaveManagementErrorCode errorCode)
    {
        if (errorCode == LeaveManagementErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new LeaveRequestsResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            LeaveRequests: []);
    }
}
