using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .HumanResources.Employees.UpdateEmployee;

public sealed record UpdateEmployeeCommand(
    int EmployeeId,
    int? UserId,
    int DepartmentId,
    string? IdentificationType,
    string? IdentificationNumber,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    DateTime? BirthDate,
    DateTime? HireDate,
    DateTime? TerminationDate,
    string? JobTitle,
    decimal? BaseSalary,
    string? ProfileImagePath,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
