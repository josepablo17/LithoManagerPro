namespace LithoManager.Application.Features
    .Documents.GetDocumentTypes;

public sealed record GetDocumentTypesQuery(
    int ActorUserId,
    bool? IsActive);
