namespace LithoManager.Application.Features.Documents;

public sealed class DocumentTypeData
{
    public int DocumentTypeId { get; init; }

    public string DocumentTypeCode { get; init; } =
        string.Empty;

    public string Name { get; init; } =
        string.Empty;

    public string? Description { get; init; }

    public bool DefaultIsVisibleToEmployee { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public int? CreatedByUserId { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public int? UpdatedByUserId { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
