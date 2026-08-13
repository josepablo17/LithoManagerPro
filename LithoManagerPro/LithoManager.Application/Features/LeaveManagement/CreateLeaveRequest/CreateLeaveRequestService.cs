using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .LeaveManagement.CreateLeaveRequest;

public sealed class CreateLeaveRequestService
    : ICreateLeaveRequestService
{
    private readonly ILeaveManagementRepository
        _leaveManagementRepository;

    public CreateLeaveRequestService(
        ILeaveManagementRepository leaveManagementRepository)
    {
        ArgumentNullException.ThrowIfNull(
            leaveManagementRepository);

        _leaveManagementRepository =
            leaveManagementRepository;
    }

    public async Task<LeaveRequestResult> CreateAsync(
        CreateLeaveRequestCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        string leaveTypeCode =
            LeaveManagementValidation.NormalizeLeaveTypeCode(
                command.LeaveTypeCode);

        if (!LeaveManagementValidation
                .IsValidLeaveRequestDates(
                    command.StartDate,
                    command.EndDate)
            || !LeaveManagementValidation
                .IsValidMutationRequest(
                    command.ActorUserId,
                    command.RequestContext)
            || !LeaveManagementValidation
                .IsValidLeaveTypeCode(
                    leaveTypeCode))
        {
            return LeaveRequestResult.Failure(
                LeaveManagementErrorCode.InvalidRequest);
        }

        try
        {
            LeaveRequestData leaveRequest =
                await _leaveManagementRepository
                    .CreateLeaveRequestAsync(
                        startDate:
                            command.StartDate!.Value,
                        endDate:
                            command.EndDate!.Value,
                        actorUserId:
                            command.ActorUserId,
                        leaveTypeCode:
                            leaveTypeCode,
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
