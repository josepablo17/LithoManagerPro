namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed class EmployeeIdentificationTypeData
{
    public string IdentificationType { get; init; } =
        string.Empty;

    public string Name { get; init; } =
        string.Empty;

    public int MinLength { get; init; }

    public int MaxLength { get; init; }

    public bool IsNumericOnly { get; init; }

    public bool AllowsLeadingZero { get; init; }

    public int SortOrder { get; init; }
}
