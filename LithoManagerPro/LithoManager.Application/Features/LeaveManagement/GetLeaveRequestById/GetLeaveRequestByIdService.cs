using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .LeaveManagement.GetLeaveRequestById;

public sealed class GetLeaveRequestByIdService
    : IGetLeaveRequestByIdService
{
    private readonly ILeaveManagementRepository
        _leaveManagementRepository;

    public GetLeaveRequestByIdService(
        ILeaveManagementRepository leaveManagementRepository)
    {
        ArgumentNullException.ThrowIfNull(
            leaveManagementRepository);

        _leaveManagementRepository =
            leaveManagementRepository;
    }

    public async Task<LeaveRequestResult> GetAsync(
        int leaveRequestId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (leaveRequestId <= 0
            || actorUserId <= 0)
        {
            return LeaveRequestResult.Failure(
                LeaveManagementErrorCode.InvalidRequest);
        }

        try
        {
            LeaveRequestData? leaveRequest =
                await _leaveManagementRepository
                    .GetLeaveRequestByIdAsync(
                        leaveRequestId,
                        actorUserId,
                        cancellationToken);

            if (leaveRequest is null)
            {
                return LeaveRequestResult.Failure(
                    LeaveManagementErrorCode
                        .LeaveRequestNotFound);
            }

            return LeaveRequestResult.Success(
                LeaveManagementMapper.Map(leaveRequest));
        }
        catch (LeaveManagementPersistenceException exception)
        {
            return LeaveRequestResult.Failure(
                exception.ErrorCode);
        }
    }
}
