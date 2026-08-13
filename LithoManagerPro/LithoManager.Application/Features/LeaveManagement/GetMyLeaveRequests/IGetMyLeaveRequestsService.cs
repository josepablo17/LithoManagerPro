namespace LithoManager.Application.Features
    .LeaveManagement.GetMyLeaveRequests;

public interface IGetMyLeaveRequestsService
{
    Task<LeaveRequestsResult> GetAsync(
        GetMyLeaveRequestsQuery query,
        CancellationToken cancellationToken);
}
