using System.ComponentModel.DataAnnotations;

namespace LithoManager.Api.Contracts
    .HumanResources.Employees;

public sealed class UpdateEmployeeRequest
{
    public int? UserId { get; init; }

    [Required]
    public int? DepartmentId { get; init; }

    [Required]
    [StringLength(30)]
    public string IdentificationType { get; init; } =
        string.Empty;

    [Required]
    [StringLength(20)]
    public string IdentificationNumber { get; init; } =
        string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; init; } =
        string.Empty;

    [Required]
    [StringLength(150)]
    public string LastName { get; init; } =
        string.Empty;

    [StringLength(8)]
    [RegularExpression(@"^\d{8}$")]
    public string? PhoneNumber { get; init; }

    public DateTime? BirthDate { get; init; }

    [Required]
    public DateTime? HireDate { get; init; }

    public DateTime? TerminationDate { get; init; }

    [Required]
    [StringLength(100)]
    public string JobTitle { get; init; } =
        string.Empty;

    [Required]
    public decimal? BaseSalary { get; init; }

    [StringLength(500)]
    public string? ProfileImagePath { get; init; }

    [Required]
    public string ExpectedRowVersion { get; init; } =
        string.Empty;
}
