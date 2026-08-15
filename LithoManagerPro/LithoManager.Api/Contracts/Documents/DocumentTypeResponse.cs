namespace LithoManager.Api.Contracts.Documents;

public sealed record DocumentTypeResponse(
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
    string RowVersion);
