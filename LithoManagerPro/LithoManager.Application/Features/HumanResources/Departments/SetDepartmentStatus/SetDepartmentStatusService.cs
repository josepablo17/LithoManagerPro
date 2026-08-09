using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Departments.SetDepartmentStatus;

public sealed class SetDepartmentStatusService
    : ISetDepartmentStatusService
{
    private readonly IDepartmentRepository
        _departmentRepository;

    public SetDepartmentStatusService(
        IDepartmentRepository departmentRepository)
    {
        ArgumentNullException.ThrowIfNull(
            departmentRepository);

        _departmentRepository =
            departmentRepository;
    }

    public async Task<DepartmentResult> SetAsync(
        SetDepartmentStatusCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (command.DepartmentId <= 0
            || !DepartmentValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext)
            || !DepartmentValidation.IsValidRowVersion(
                command.ExpectedRowVersion))
        {
            return DepartmentResult.Failure(
                DepartmentErrorCode.InvalidRequest);
        }

        try
        {
            DepartmentData department =
                await _departmentRepository
                    .SetDepartmentStatusAsync(
                        departmentId:
                            command.DepartmentId,
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

            return DepartmentResult.Success(
                DepartmentMapper.Map(department));
        }
        catch (DepartmentPersistenceException exception)
        {
            return DepartmentResult.Failure(
                exception.ErrorCode);
        }
    }
}
