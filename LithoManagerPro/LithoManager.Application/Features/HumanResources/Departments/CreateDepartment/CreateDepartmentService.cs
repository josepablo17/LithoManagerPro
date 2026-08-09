using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Departments.CreateDepartment;

public sealed class CreateDepartmentService
    : ICreateDepartmentService
{
    private readonly IDepartmentRepository
        _departmentRepository;

    public CreateDepartmentService(
        IDepartmentRepository departmentRepository)
    {
        ArgumentNullException.ThrowIfNull(
            departmentRepository);

        _departmentRepository =
            departmentRepository;
    }

    public async Task<DepartmentResult> CreateAsync(
        CreateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!DepartmentValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext)
            || !DepartmentValidation.IsValidDepartmentCode(
                command.DepartmentCode)
            || !DepartmentValidation.IsValidName(
                command.Name)
            || !DepartmentValidation.IsValidDescription(
                command.Description))
        {
            return DepartmentResult.Failure(
                DepartmentErrorCode.InvalidRequest);
        }

        try
        {
            DepartmentData department =
                await _departmentRepository
                    .CreateDepartmentAsync(
                        departmentCode:
                            command.DepartmentCode!,
                        name:
                            command.Name!,
                        description:
                            command.Description,
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
