namespace LithoManager.Application.Features.Documents;

public sealed record EmployeeDocumentResult(
    bool IsSuccessful,
    DocumentErrorCode ErrorCode,
    EmployeeDocumentInfo? EmployeeDocument)
{
    public static EmployeeDocumentResult Success(
        EmployeeDocumentInfo employeeDocument)
    {
        ArgumentNullException.ThrowIfNull(employeeDocument);

        return new EmployeeDocumentResult(
            IsSuccessful: true,
            ErrorCode: DocumentErrorCode.None,
            EmployeeDocument: employeeDocument);
    }

    public static EmployeeDocumentResult Failure(
        DocumentErrorCode errorCode)
    {
        if (errorCode == DocumentErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new EmployeeDocumentResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            EmployeeDocument: null);
    }
}
