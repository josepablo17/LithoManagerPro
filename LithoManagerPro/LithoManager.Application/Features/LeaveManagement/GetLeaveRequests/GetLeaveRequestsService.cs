using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .LeaveManagement.GetLeaveRequests;

public sealed class GetLeaveRequestsService
    : IGetLeaveRequestsService
{
    private readonly ILeaveManagementRepository
        _leaveManagementRepository;

    public GetLeaveRequestsService(
        ILeaveManagementRepository leaveManagementRepository)
    {
        ArgumentNullException.ThrowIfNull(
            leaveManagementRepository);

        _leaveManagementRepository =
            leaveManagementRepository;
    }

    public async Task<LeaveRequestsResult> GetAsync(
        GetLeaveRequestsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        string? leaveRequestStatusCode =
            string.IsNullOrWhiteSpace(
                query.LeaveRequestStatusCode)
                    ? null
                    : query.LeaveRequestStatusCode.Trim();

        if (query.ActorUserId <= 0
            || query.EmployeeId is <= 0
            || query.DepartmentId is <= 0
            || !LeaveManagementValidation.IsValidStatusCode(
                leaveRequestStatusCode)
            || !LeaveManagementValidation.IsValidDateRange(
                query.StartDateFrom,
                query.StartDateTo)
            || !LeaveManagementValidation.IsValidSearchTerm(
                query.SearchTerm))
        {
            return LeaveRequestsResult.Failure(
                LeaveManagementErrorCode.InvalidRequest);
        }

        try
        {
            IReadOnlyList<LeaveRequestData> leaveRequests =
                await _leaveManagementRepository
                    .GetLeaveRequestsAsync(
                        actorUserId:
                            query.ActorUserId,
                        leaveRequestStatusCode:
                            leaveRequestStatusCode,
                        employeeId:
                            query.EmployeeId,
                        departmentId:
                            query.DepartmentId,
                        startDateFrom:
                            query.StartDateFrom,
                        startDateTo:
                            query.StartDateTo,
                        searchTerm:
                            query.SearchTerm,
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
