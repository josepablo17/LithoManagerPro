namespace LithoManager.Application.Features.Documents;

public sealed record DocumentTypeInfo(
    int DocumentTypeId,
    string DocumentTypeCode,
    string Name,
    string? Description,
    bool DefaultIsVisibleToEmployee,
    bool IsActive,
    DateTime CreatedAtUtc,
    int? CreatedByUserId,
    DateTime? UpdatedAtUtc,
    int? UpdatedByUserId,
    byte[] RowVersion);
