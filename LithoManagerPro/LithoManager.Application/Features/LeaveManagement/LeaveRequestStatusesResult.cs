namespace LithoManager.Application.Features.LeaveManagement;

public sealed record LeaveRequestStatusesResult(
    bool IsSuccessful,
    LeaveManagementErrorCode ErrorCode,
    IReadOnlyList<LeaveRequestStatusInfo> LeaveRequestStatuses)
{
    public static LeaveRequestStatusesResult Success(
        IReadOnlyList<LeaveRequestStatusInfo>
            leaveRequestStatuses)
    {
        ArgumentNullException.ThrowIfNull(
            leaveRequestStatuses);

        return new LeaveRequestStatusesResult(
            IsSuccessful: true,
            ErrorCode: LeaveManagementErrorCode.None,
            LeaveRequestStatuses: leaveRequestStatuses);
    }

    public static LeaveRequestStatusesResult Failure(
        LeaveManagementErrorCode errorCode)
    {
        if (errorCode == LeaveManagementErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new LeaveRequestStatusesResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            LeaveRequestStatuses: []);
    }
}
