namespace LithoManager.Application.Features.Documents;

public enum DocumentErrorCode
{
    None = 0,
    InvalidRequest = 1,
    AccessNotAvailable = 2,
    EmployeeNotFound = 3,
    EmployeeRecordNotFound = 4,
    DocumentTypeNotFound = 5,
    EmployeeDocumentNotFound = 6,
    DuplicateStorageKey = 7,
    ConcurrencyConflict = 8
}
