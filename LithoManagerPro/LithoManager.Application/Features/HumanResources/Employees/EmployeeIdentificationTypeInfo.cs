namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed record EmployeeIdentificationTypeInfo(
    string IdentificationType,
    string Name,
    int MinLength,
    int MaxLength,
    bool IsNumericOnly,
    bool AllowsLeadingZero,
    int SortOrder);
