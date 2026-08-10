using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features
    .HumanResources.Employees.CreateEmployee;

public sealed record CreateEmployeeCommand(
    int? UserId,
    int DepartmentId,
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
    int ActorUserId,
    AuthenticationRequestContext RequestContext);
