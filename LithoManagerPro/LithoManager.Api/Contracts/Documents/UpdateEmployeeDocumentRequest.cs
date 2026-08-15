using System.ComponentModel.DataAnnotations;

namespace LithoManager.Api.Contracts.Documents;

public sealed class UpdateEmployeeDocumentRequest
{
    [Required]
    public int? DocumentTypeId { get; init; }

    [Required]
    [MaxLength(150)]
    public string Title { get; init; } =
        string.Empty;

    [MaxLength(500)]
    public string? Description { get; init; }

    public DateTime? IssuedDate { get; init; }

    public DateTime? ExpirationDate { get; init; }

    public bool IsVisibleToEmployee { get; init; }

    [Required]
    public string ExpectedRowVersion { get; init; } =
        string.Empty;
}
