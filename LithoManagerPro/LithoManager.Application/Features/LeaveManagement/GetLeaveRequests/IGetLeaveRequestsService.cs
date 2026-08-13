namespace LithoManager.Application.Features
    .LeaveManagement.GetLeaveRequests;

public interface IGetLeaveRequestsService
{
    Task<LeaveRequestsResult> GetAsync(
        GetLeaveRequestsQuery query,
        CancellationToken cancellationToken);
}
