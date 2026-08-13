namespace LithoManager.Application.Features
    .LeaveManagement.GetLeaveRequestById;

public interface IGetLeaveRequestByIdService
{
    Task<LeaveRequestResult> GetAsync(
        int leaveRequestId,
        int actorUserId,
        CancellationToken cancellationToken);
}
