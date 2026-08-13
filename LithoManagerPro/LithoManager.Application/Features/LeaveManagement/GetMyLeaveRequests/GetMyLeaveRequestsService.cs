using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .LeaveManagement.GetMyLeaveRequests;

public sealed class GetMyLeaveRequestsService
    : IGetMyLeaveRequestsService
{
    private readonly ILeaveManagementRepository
        _leaveManagementRepository;

    public GetMyLeaveRequestsService(
        ILeaveManagementRepository leaveManagementRepository)
    {
        ArgumentNullException.ThrowIfNull(
            leaveManagementRepository);

        _leaveManagementRepository =
            leaveManagementRepository;
    }

    public async Task<LeaveRequestsResult> GetAsync(
        GetMyLeaveRequestsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ActorUserId <= 0
            || !LeaveManagementValidation.IsValidStatusCode(
                query.LeaveRequestStatusCode)
            || !LeaveManagementValidation.IsValidDateRange(
                query.StartDateFrom,
                query.StartDateTo))
        {
            return LeaveRequestsResult.Failure(
                LeaveManagementErrorCode.InvalidRequest);
        }

        try
        {
            IReadOnlyList<LeaveRequestData> leaveRequests =
                await _leaveManagementRepository
                    .GetMyLeaveRequestsAsync(
                        actorUserId:
                            query.ActorUserId,
                        leaveRequestStatusCode:
                            query.LeaveRequestStatusCode,
                        startDateFrom:
                            query.StartDateFrom,
                        startDateTo:
                            query.StartDateTo,
                        cancellationToken:
                            cancellationToken);

            return LeaveRequestsResult.Success(
                leaveRequests
                    .Select(LeaveManagementMapper.Map)
                    .ToList());
        }
        catch (LeaveManagementPersistenceException exception)
        {
            return LeaveRequestsResult.Failure(
                exception.ErrorCode);
        }
    }
}
