namespace LithoManager.Application.Features.Documents;

public sealed record EmployeeDocumentDownloadContextResult(
    bool IsSuccessful,
    DocumentErrorCode ErrorCode,
    EmployeeDocumentDownloadContextInfo? DownloadContext)
{
    public static EmployeeDocumentDownloadContextResult Success(
        EmployeeDocumentDownloadContextInfo downloadContext)
    {
        ArgumentNullException.ThrowIfNull(downloadContext);

        return new EmployeeDocumentDownloadContextResult(
            IsSuccessful: true,
            ErrorCode: DocumentErrorCode.None,
            DownloadContext: downloadContext);
    }

    public static EmployeeDocumentDownloadContextResult Failure(
        DocumentErrorCode errorCode)
    {
        if (errorCode == DocumentErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new EmployeeDocumentDownloadContextResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            DownloadContext: null);
    }
}
