using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .LeaveManagement.GetEmployeeLeaveBalance;

public sealed class GetEmployeeLeaveBalanceService
    : IGetEmployeeLeaveBalanceService
{
    private readonly ILeaveManagementRepository
        _leaveManagementRepository;

    public GetEmployeeLeaveBalanceService(
        ILeaveManagementRepository leaveManagementRepository)
    {
        ArgumentNullException.ThrowIfNull(
            leaveManagementRepository);

        _leaveManagementRepository =
            leaveManagementRepository;
    }

    public async Task<EmployeeLeaveBalanceResult> GetAsync(
        GetEmployeeLeaveBalanceQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        string leaveTypeCode =
            LeaveManagementValidation.NormalizeLeaveTypeCode(
                query.LeaveTypeCode);

        if (query.ActorUserId <= 0
            || query.EmployeeId is <= 0
            || !LeaveManagementValidation.IsValidLeaveTypeCode(
                leaveTypeCode))
        {
            return EmployeeLeaveBalanceResult.Failure(
                LeaveManagementErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeLeaveBalanceData? balance =
                await _leaveManagementRepository
                    .GetEmployeeLeaveBalanceAsync(
                        employeeId:
                            query.EmployeeId,
                        leaveTypeCode:
                            leaveTypeCode,
                        actorUserId:
                            query.ActorUserId,
                        cancellationToken:
                            cancellationToken);

            if (balance is null)
            {
                return EmployeeLeaveBalanceResult.Failure(
                    LeaveManagementErrorCode
                        .LeaveBalanceNotFound);
            }

            return EmployeeLeaveBalanceResult.Success(
                LeaveManagementMapper.Map(balance));
        }
        catch (LeaveManagementPersistenceException exception)
        {
            return EmployeeLeaveBalanceResult.Failure(
                exception.ErrorCode);
        }
    }
}
