namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed record EmployeesResult(
    bool IsSuccessful,
    EmployeeErrorCode ErrorCode,
    IReadOnlyList<EmployeeInfo> Employees)
{
    public static EmployeesResult Success(
        IReadOnlyList<EmployeeInfo> employees)
    {
        ArgumentNullException.ThrowIfNull(employees);

        return new EmployeesResult(
            IsSuccessful: true,
            ErrorCode: EmployeeErrorCode.None,
            Employees: employees);
    }

    public static EmployeesResult Failure(
        EmployeeErrorCode errorCode)
    {
        if (errorCode == EmployeeErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new EmployeesResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            Employees: []);
    }
}
