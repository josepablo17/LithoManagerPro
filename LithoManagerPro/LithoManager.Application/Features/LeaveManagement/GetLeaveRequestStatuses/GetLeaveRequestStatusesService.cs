using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .LeaveManagement.GetLeaveRequestStatuses;

public sealed class GetLeaveRequestStatusesService
    : IGetLeaveRequestStatusesService
{
    private readonly ILeaveManagementRepository
        _leaveManagementRepository;

    public GetLeaveRequestStatusesService(
        ILeaveManagementRepository leaveManagementRepository)
    {
        ArgumentNullException.ThrowIfNull(
            leaveManagementRepository);

        _leaveManagementRepository =
            leaveManagementRepository;
    }

    public async Task<LeaveRequestStatusesResult> GetAsync(
        GetLeaveRequestStatusesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IReadOnlyList<LeaveRequestStatusData> statuses =
            await _leaveManagementRepository
                .GetLeaveRequestStatusesAsync(
                    query.IsActive,
                    cancellationToken);

        return LeaveRequestStatusesResult.Success(
            statuses
                .Select(LeaveManagementMapper.Map)
                .ToList());
    }
}
