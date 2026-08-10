namespace LithoManager.Application.Features
    .HumanResources.Employees.GetEmployees;

public sealed record GetEmployeesQuery(
    string? SearchTerm,
    int? DepartmentId,
    bool? IsActive);
