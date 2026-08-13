namespace LithoManager.Application.Features
    .LeaveManagement.CreateLeaveRequest;

public interface ICreateLeaveRequestService
{
    Task<LeaveRequestResult> CreateAsync(
        CreateLeaveRequestCommand command,
        CancellationToken cancellationToken);
}
