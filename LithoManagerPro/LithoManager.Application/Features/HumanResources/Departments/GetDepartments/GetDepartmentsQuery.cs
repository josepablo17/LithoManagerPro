namespace LithoManager.Application.Features
    .HumanResources.Departments.GetDepartments;

public sealed record GetDepartmentsQuery(
    string? SearchTerm,
    bool? IsActive);
