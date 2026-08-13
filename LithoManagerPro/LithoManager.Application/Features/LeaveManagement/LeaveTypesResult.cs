namespace LithoManager.Application.Features.LeaveManagement;

public sealed record LeaveTypesResult(
    bool IsSuccessful,
    LeaveManagementErrorCode ErrorCode,
    IReadOnlyList<LeaveTypeInfo> LeaveTypes)
{
    public static LeaveTypesResult Success(
        IReadOnlyList<LeaveTypeInfo> leaveTypes)
    {
        ArgumentNullException.ThrowIfNull(leaveTypes);

        return new LeaveTypesResult(
            IsSuccessful: true,
            ErrorCode: LeaveManagementErrorCode.None,
            LeaveTypes: leaveTypes);
    }

    public static LeaveTypesResult Failure(
        LeaveManagementErrorCode errorCode)
    {
        if (errorCode == LeaveManagementErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new LeaveTypesResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            LeaveTypes: []);
    }
}
