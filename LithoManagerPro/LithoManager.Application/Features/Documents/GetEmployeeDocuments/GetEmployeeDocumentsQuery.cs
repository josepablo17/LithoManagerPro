namespace LithoManager.Application.Features
    .Documents.GetEmployeeDocuments;

public sealed record GetEmployeeDocumentsQuery(
    int ActorUserId,
    int? EmployeeId,
    int? DocumentTypeId,
    bool? IsActive,
    bool? IsVisibleToEmployee,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc,
    string? SearchTerm);
