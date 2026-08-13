using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .LeaveManagement.AdjustEmployeeLeaveBalance;

public sealed class AdjustEmployeeLeaveBalanceService
    : IAdjustEmployeeLeaveBalanceService
{
    private readonly ILeaveManagementRepository
        _leaveManagementRepository;

    public AdjustEmployeeLeaveBalanceService(
        ILeaveManagementRepository leaveManagementRepository)
    {
        ArgumentNullException.ThrowIfNull(
            leaveManagementRepository);

        _leaveManagementRepository =
            leaveManagementRepository;
    }

    public async Task<EmployeeLeaveBalanceResult> AdjustAsync(
        AdjustEmployeeLeaveBalanceCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        string leaveTypeCode =
            LeaveManagementValidation.NormalizeLeaveTypeCode(
                command.LeaveTypeCode);

        if (command.EmployeeId <= 0
            || command.AdjustedDaysDelta == 0
            || !LeaveManagementValidation
                .IsValidMutationRequest(
                    command.ActorUserId,
                    command.RequestContext)
            || !LeaveManagementValidation
                .IsValidLeaveTypeCode(
                    leaveTypeCode))
        {
            return EmployeeLeaveBalanceResult.Failure(
                LeaveManagementErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeLeaveBalanceData balance =
                await _leaveManagementRepository
                    .AdjustEmployeeLeaveBalanceAsync(
                        employeeId:
                            command.EmployeeId,
                        leaveTypeCode:
                            leaveTypeCode,
                        adjustedDaysDelta:
                            command.AdjustedDaysDelta,
                        actorUserId:
                            command.ActorUserId,
                        requestContext:
                            command.RequestContext,
                        cancellationToken:
                            cancellationToken);

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
