namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed class EmployeePersistenceException
    : Exception
{
    public EmployeePersistenceException(
        EmployeeErrorCode errorCode,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        if (errorCode == EmployeeErrorCode.None)
        {
            throw new ArgumentException(
                "A persistence exception must contain an error code.",
                nameof(errorCode));
        }

        ErrorCode = errorCode;
    }

    public EmployeeErrorCode ErrorCode { get; }
}
