using System.ComponentModel.DataAnnotations;

namespace LithoManager.Api.Contracts
    .HumanResources.Employees;

public sealed class SetEmployeeStatusRequest
{
    public bool IsActive { get; init; }

    [Required]
    public string ExpectedRowVersion { get; init; } =
        string.Empty;
}
