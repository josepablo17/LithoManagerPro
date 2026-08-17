namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed class AssignableEmployeeUserData
{
    public int UserId { get; init; }

    public string EmailAddress { get; init; } =
        string.Empty;

    public int RoleId { get; init; }

    public string RoleCode { get; init; } =
        string.Empty;

    public string RoleName { get; init; } =
        string.Empty;

    public int? AssignedEmployeeId { get; init; }

    public string? AssignedEmployeeFirstName { get; init; }

    public string? AssignedEmployeeLastName { get; init; }
}
