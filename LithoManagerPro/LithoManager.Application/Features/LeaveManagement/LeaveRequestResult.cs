namespace LithoManager.Application.Features.LeaveManagement;

public sealed record LeaveRequestResult(
    bool IsSuccessful,
    LeaveManagementErrorCode ErrorCode,
    LeaveRequestInfo? LeaveRequest)
{
    public static LeaveRequestResult Success(
        LeaveRequestInfo leaveRequest)
    {
        ArgumentNullException.ThrowIfNull(leaveRequest);

        return new LeaveRequestResult(
            IsSuccessful: true,
            ErrorCode: LeaveManagementErrorCode.None,
            LeaveRequest: leaveRequest);
    }

    public static LeaveRequestResult Failure(
        LeaveManagementErrorCode errorCode)
    {
        if (errorCode == LeaveManagementErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new LeaveRequestResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            LeaveRequest: null);
    }
}
