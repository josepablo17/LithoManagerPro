namespace LithoManager.Application.Features.Documents;

public sealed record DocumentTypesResult(
    bool IsSuccessful,
    DocumentErrorCode ErrorCode,
    IReadOnlyList<DocumentTypeInfo> DocumentTypes)
{
    public static DocumentTypesResult Success(
        IReadOnlyList<DocumentTypeInfo> documentTypes)
    {
        ArgumentNullException.ThrowIfNull(documentTypes);

        return new DocumentTypesResult(
            IsSuccessful: true,
            ErrorCode: DocumentErrorCode.None,
            DocumentTypes: documentTypes);
    }

    public static DocumentTypesResult Failure(
        DocumentErrorCode errorCode)
    {
        if (errorCode == DocumentErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new DocumentTypesResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            DocumentTypes: []);
    }
}
