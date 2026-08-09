using System.ComponentModel.DataAnnotations;

namespace LithoManager.Api.Contracts
    .HumanResources.Departments;

public sealed class SetDepartmentStatusRequest
{
    public bool IsActive { get; init; }

    [Required]
    public string ExpectedRowVersion { get; init; } =
        string.Empty;
}
