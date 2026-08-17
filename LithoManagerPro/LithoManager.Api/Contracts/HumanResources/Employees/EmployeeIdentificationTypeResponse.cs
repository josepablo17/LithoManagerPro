namespace LithoManager.Api.Contracts
    .HumanResources.Employees;

public sealed record EmployeeIdentificationTypeResponse(
    string IdentificationType,
    string Name,
    int MinLength,
    int MaxLength,
    bool IsNumericOnly,
    bool AllowsLeadingZero,
    int SortOrder);
