using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .HumanResources.Departments.GetDepartments;

public sealed class GetDepartmentsService
    : IGetDepartmentsService
{
    private readonly IDepartmentRepository
        _departmentRepository;

    public GetDepartmentsService(
        IDepartmentRepository departmentRepository)
    {
        ArgumentNullException.ThrowIfNull(
            departmentRepository);

        _departmentRepository =
            departmentRepository;
    }

    public async Task<DepartmentsResult> GetAsync(
        GetDepartmentsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!DepartmentValidation.IsValidSearchTerm(
                query.SearchTerm))
        {
            return DepartmentsResult.Failure(
                DepartmentErrorCode.InvalidRequest);
        }

        IReadOnlyList<DepartmentData> departments =
            await _departmentRepository
                .GetDepartmentsAsync(
                    query.SearchTerm,
                    query.IsActive,
                    cancellationToken);

        return DepartmentsResult.Success(
            departments
                .Select(DepartmentMapper.Map)
                .ToList());
    }
}
