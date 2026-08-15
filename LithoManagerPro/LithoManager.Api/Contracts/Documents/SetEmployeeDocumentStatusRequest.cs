using System.ComponentModel.DataAnnotations;

namespace LithoManager.Api.Contracts.Documents;

public sealed class SetEmployeeDocumentStatusRequest
{
    public bool IsActive { get; init; }

    [Required]
    public string ExpectedRowVersion { get; init; } =
        string.Empty;
}
