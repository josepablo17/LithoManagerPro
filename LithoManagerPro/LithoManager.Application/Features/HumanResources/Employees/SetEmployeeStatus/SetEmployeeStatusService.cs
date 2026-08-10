using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Employees.SetEmployeeStatus;

public sealed class SetEmployeeStatusService
    : ISetEmployeeStatusService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    public SetEmployeeStatusService(
        IEmployeeRepository employeeRepository)
    {
        ArgumentNullException.ThrowIfNull(
            employeeRepository);

        _employeeRepository =
            employeeRepository;
    }

    public async Task<EmployeeResult> SetAsync(
        SetEmployeeStatusCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (command.EmployeeId <= 0
            || !EmployeeValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext)
            || !EmployeeValidation.IsValidRowVersion(
                command.ExpectedRowVersion))
        {
            return EmployeeResult.Failure(
                EmployeeErrorCode.InvalidRequest);
        }

        try
        {
            EmployeeData employee =
                await _employeeRepository.SetEmployeeStatusAsync(
                    employeeId:
                        command.EmployeeId,
                    isActive:
                        command.IsActive,
                    expectedRowVersion:
                        command.ExpectedRowVersion!,
                    actorUserId:
                        command.ActorUserId,
                    requestContext:
                        command.RequestContext,
                    cancellationToken:
                        cancellationToken);

            return EmployeeResult.Success(
                EmployeeMapper.Map(employee));
        }
        catch (EmployeePersistenceException exception)
        {
            return EmployeeResult.Failure(
                exception.ErrorCode);
        }
    }
}
