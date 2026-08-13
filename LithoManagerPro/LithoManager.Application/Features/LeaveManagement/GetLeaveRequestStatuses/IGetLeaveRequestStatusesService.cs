namespace LithoManager.Application.Features
    .LeaveManagement.GetLeaveRequestStatuses;

public interface IGetLeaveRequestStatusesService
{
    Task<LeaveRequestStatusesResult> GetAsync(
        GetLeaveRequestStatusesQuery query,
        CancellationToken cancellationToken);
}
