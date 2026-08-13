namespace LithoManager.Api.Contracts.LeaveManagement;

public sealed class CancelLeaveRequestRequest
{
    public string ExpectedRowVersion { get; init; } =
        string.Empty;
}
