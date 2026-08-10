namespace LithoManager.Application.Features
    .HumanResources.Departments;

public sealed class DepartmentPersistenceException
    : Exception
{
    public DepartmentPersistenceException(
        DepartmentErrorCode errorCode,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        if (errorCode == DepartmentErrorCode.None)
        {
            throw new ArgumentException(
                "A persistence exception must contain an error code.",
                nameof(errorCode));
        }

        ErrorCode = errorCode;
    }

    public DepartmentErrorCode ErrorCode { get; }
}
