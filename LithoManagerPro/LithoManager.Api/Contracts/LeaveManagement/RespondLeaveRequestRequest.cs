namespace LithoManager.Api.Contracts.LeaveManagement;

public sealed class RespondLeaveRequestRequest
{
    public bool IsApproved { get; init; }

    public string ExpectedRowVersion { get; init; } =
        string.Empty;
}
