using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .LeaveManagement.GetLeaveTypes;

public sealed class GetLeaveTypesService
    : IGetLeaveTypesService
{
    private readonly ILeaveManagementRepository
        _leaveManagementRepository;

    public GetLeaveTypesService(
        ILeaveManagementRepository leaveManagementRepository)
    {
        ArgumentNullException.ThrowIfNull(
            leaveManagementRepository);

        _leaveManagementRepository =
            leaveManagementRepository;
    }

    public async Task<LeaveTypesResult> GetAsync(
        GetLeaveTypesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IReadOnlyList<LeaveTypeData> leaveTypes =
            await _leaveManagementRepository.GetLeaveTypesAsync(
                query.IsActive,
                cancellationToken);

        return LeaveTypesResult.Success(
            leaveTypes
                .Select(LeaveManagementMapper.Map)
                .ToList());
    }
}
