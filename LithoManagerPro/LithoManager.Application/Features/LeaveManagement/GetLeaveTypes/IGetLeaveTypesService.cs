namespace LithoManager.Application.Features
    .LeaveManagement.GetLeaveTypes;

public interface IGetLeaveTypesService
{
    Task<LeaveTypesResult> GetAsync(
        GetLeaveTypesQuery query,
        CancellationToken cancellationToken);
}
