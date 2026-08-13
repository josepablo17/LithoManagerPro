using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .LeaveManagement.CancelLeaveRequest;

public sealed class CancelLeaveRequestService
    : ICancelLeaveRequestService
{
    private readonly ILeaveManagementRepository
        _leaveManagementRepository;

    public CancelLeaveRequestService(
        ILeaveManagementRepository leaveManagementRepository)
    {
        ArgumentNullException.ThrowIfNull(
            leaveManagementRepository);

        _leaveManagementRepository =
            leaveManagementRepository;
    }

    public async Task<LeaveRequestResult> CancelAsync(
        CancelLeaveRequestCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (command.LeaveRequestId <= 0
            || !LeaveManagementValidation
                .IsValidMutationRequest(
                    command.ActorUserId,
                    command.RequestContext)
            || !LeaveManagementValidation.IsValidRowVersion(
                command.ExpectedRowVersion))
        {
            return LeaveRequestResult.Failure(
                LeaveManagementErrorCode.InvalidRequest);
        }

        try
        {
            LeaveRequestData leaveRequest =
                await _leaveManagementRepository
                    .CancelLeaveRequestAsync(
                        leaveRequestId:
                            command.LeaveRequestId,
                        expectedRowVersion:
                            command.ExpectedRowVersion!,
                        actorUserId:
                            command.ActorUserId,
                        requestContext:
                            command.RequestContext,
                        cancellationToken:
                            cancellationToken);

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
