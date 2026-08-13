using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .LeaveManagement.RespondLeaveRequest;

public sealed class RespondLeaveRequestService
    : IRespondLeaveRequestService
{
    private readonly ILeaveManagementRepository
        _leaveManagementRepository;

    public RespondLeaveRequestService(
        ILeaveManagementRepository leaveManagementRepository)
    {
        ArgumentNullException.ThrowIfNull(
            leaveManagementRepository);

        _leaveManagementRepository =
            leaveManagementRepository;
    }

    public async Task<LeaveRequestResult> RespondAsync(
        RespondLeaveRequestCommand command,
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
                    .RespondLeaveRequestAsync(
                        leaveRequestId:
                            command.LeaveRequestId,
                        isApproved:
                            command.IsApproved,
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
