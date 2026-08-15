namespace LithoManager.Application.Features.Documents;

public sealed record EmployeeDocumentsResult(
    bool IsSuccessful,
    DocumentErrorCode ErrorCode,
    IReadOnlyList<EmployeeDocumentInfo> EmployeeDocuments)
{
    public static EmployeeDocumentsResult Success(
        IReadOnlyList<EmployeeDocumentInfo> employeeDocuments)
    {
        ArgumentNullException.ThrowIfNull(employeeDocuments);

        return new EmployeeDocumentsResult(
            IsSuccessful: true,
            ErrorCode: DocumentErrorCode.None,
            EmployeeDocuments: employeeDocuments);
    }

    public static EmployeeDocumentsResult Failure(
        DocumentErrorCode errorCode)
    {
        if (errorCode == DocumentErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new EmployeeDocumentsResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            EmployeeDocuments: []);
    }
}
