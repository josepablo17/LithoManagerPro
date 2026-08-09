using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Departments.UpdateDepartment;

public sealed class UpdateDepartmentService
    : IUpdateDepartmentService
{
    private readonly IDepartmentRepository
        _departmentRepository;

    public UpdateDepartmentService(
        IDepartmentRepository departmentRepository)
    {
        ArgumentNullException.ThrowIfNull(
            departmentRepository);

        _departmentRepository =
            departmentRepository;
    }

    public async Task<DepartmentResult> UpdateAsync(
        UpdateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (command.DepartmentId <= 0
            || !DepartmentValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext)
            || !DepartmentValidation.IsValidDepartmentCode(
                command.DepartmentCode)
            || !DepartmentValidation.IsValidName(
                command.Name)
            || !DepartmentValidation.IsValidDescription(
                command.Description)
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
                    .UpdateDepartmentAsync(
                        departmentId:
                            command.DepartmentId,
                        departmentCode:
                            command.DepartmentCode!,
                        name:
                            command.Name!,
                        description:
                            command.Description,
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
