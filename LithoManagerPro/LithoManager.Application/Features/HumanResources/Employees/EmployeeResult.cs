namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed record EmployeeResult(
    bool IsSuccessful,
    EmployeeErrorCode ErrorCode,
    EmployeeInfo? Employee)
{
    public static EmployeeResult Success(
        EmployeeInfo employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        return new EmployeeResult(
            IsSuccessful: true,
            ErrorCode: EmployeeErrorCode.None,
            Employee: employee);
    }

    public static EmployeeResult Failure(
        EmployeeErrorCode errorCode)
    {
        if (errorCode == EmployeeErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new EmployeeResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            Employee: null);
    }
}
