namespace LithoManager.Application.Features.LeaveManagement;

public sealed class LeaveManagementPersistenceException
    : Exception
{
    public LeaveManagementPersistenceException(
        LeaveManagementErrorCode errorCode,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        if (errorCode == LeaveManagementErrorCode.None)
        {
            throw new ArgumentException(
                "A persistence exception must contain an error code.",
                nameof(errorCode));
        }

        ErrorCode = errorCode;
    }

    public LeaveManagementErrorCode ErrorCode { get; }
}
