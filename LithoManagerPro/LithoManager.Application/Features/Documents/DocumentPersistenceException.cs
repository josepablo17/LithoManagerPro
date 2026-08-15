namespace LithoManager.Application.Features.Documents;

public sealed class DocumentPersistenceException
    : Exception
{
    public DocumentPersistenceException(
        DocumentErrorCode errorCode,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        if (errorCode == DocumentErrorCode.None)
        {
            throw new ArgumentException(
                "A persistence exception must contain an error code.",
                nameof(errorCode));
        }

        ErrorCode = errorCode;
    }

    public DocumentErrorCode ErrorCode { get; }
}
