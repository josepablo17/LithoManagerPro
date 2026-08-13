namespace LithoManager.Application.Features
    .LeaveManagement.RespondLeaveRequest;

public interface IRespondLeaveRequestService
{
    Task<LeaveRequestResult> RespondAsync(
        RespondLeaveRequestCommand command,
        CancellationToken cancellationToken);
}
