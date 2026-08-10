using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Departments;

namespace LithoManager.Application.Abstractions.Persistence;

public interface IDepartmentRepository
{
    Task<DepartmentData> CreateDepartmentAsync(
        string departmentCode,
        string name,
        string? description,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<DepartmentData?> GetDepartmentByIdAsync(
        int departmentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DepartmentData>> GetDepartmentsAsync(
        string? searchTerm,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<DepartmentData> UpdateDepartmentAsync(
        int departmentId,
        string departmentCode,
        string name,
        string? description,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<DepartmentData> SetDepartmentStatusAsync(
        int departmentId,
        bool isActive,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);
}
