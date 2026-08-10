using System.ComponentModel.DataAnnotations;

namespace LithoManager.Api.Contracts
    .HumanResources.Departments;

public sealed class UpdateDepartmentRequest
{
    [Required]
    [StringLength(50)]
    public string DepartmentCode { get; init; } =
        string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; init; } =
        string.Empty;

    [StringLength(300)]
    public string? Description { get; init; }

    [Required]
    public string ExpectedRowVersion { get; init; } =
        string.Empty;
}
