namespace LithoManager.Application.Features
    .LeaveManagement.CancelLeaveRequest;

public interface ICancelLeaveRequestService
{
    Task<LeaveRequestResult> CancelAsync(
        CancelLeaveRequestCommand command,
        CancellationToken cancellationToken);
}
