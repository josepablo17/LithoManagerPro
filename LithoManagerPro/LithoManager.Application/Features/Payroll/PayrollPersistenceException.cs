namespace LithoManager.Application.Features.Payroll;

public sealed class PayrollPersistenceException
    : Exception
{
    public PayrollPersistenceException(
        PayrollErrorCode errorCode,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        if (errorCode == PayrollErrorCode.None)
        {
            throw new ArgumentException(
                "A persistence exception must contain an error code.",
                nameof(errorCode));
        }

        ErrorCode = errorCode;
    }

    public PayrollErrorCode ErrorCode { get; }
}
