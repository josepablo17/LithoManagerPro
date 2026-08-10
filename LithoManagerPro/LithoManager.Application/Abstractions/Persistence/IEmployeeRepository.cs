using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Employees;

namespace LithoManager.Application.Abstractions.Persistence;

public interface IEmployeeRepository
{
    Task<EmployeeData> CreateEmployeeAsync(
        int? userId,
        int departmentId,
        string identificationNumber,
        string firstName,
        string lastName,
        string? phoneNumber,
        DateTime? birthDate,
        DateTime hireDate,
        DateTime? terminationDate,
        string jobTitle,
        decimal baseSalary,
        string? profileImagePath,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<EmployeeData?> GetEmployeeByIdAsync(
        int employeeId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EmployeeData>> GetEmployeesAsync(
        string? searchTerm,
        int? departmentId,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<EmployeeData> UpdateEmployeeAsync(
        int employeeId,
        int? userId,
        int departmentId,
        string identificationNumber,
        string firstName,
        string lastName,
        string? phoneNumber,
        DateTime? birthDate,
        DateTime hireDate,
        DateTime? terminationDate,
        string jobTitle,
        decimal baseSalary,
        string? profileImagePath,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<EmployeeData> SetEmployeeStatusAsync(
        int employeeId,
        bool isActive,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);
}
