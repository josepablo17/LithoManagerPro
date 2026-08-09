using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Departments.GetDepartmentById;

public sealed class GetDepartmentByIdService
    : IGetDepartmentByIdService
{
    private readonly IDepartmentRepository
        _departmentRepository;

    public GetDepartmentByIdService(
        IDepartmentRepository departmentRepository)
    {
        ArgumentNullException.ThrowIfNull(
            departmentRepository);

        _departmentRepository =
            departmentRepository;
    }

    public async Task<DepartmentResult> GetAsync(
        int departmentId,
        CancellationToken cancellationToken)
    {
        if (departmentId <= 0)
        {
            return DepartmentResult.Failure(
                DepartmentErrorCode.InvalidRequest);
        }

        DepartmentData? department =
            await _departmentRepository
                .GetDepartmentByIdAsync(
                    departmentId,
                    cancellationToken);

        if (department is null)
        {
            return DepartmentResult.Failure(
                DepartmentErrorCode.DepartmentNotFound);
        }

        return DepartmentResult.Success(
            DepartmentMapper.Map(department));
    }
}
