namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed record EmployeeSalaryHistoryResult(
    bool IsSuccessful,
    EmployeeErrorCode ErrorCode,
    IReadOnlyList<EmployeeSalaryHistoryInfo> SalaryHistory)
{
    public static EmployeeSalaryHistoryResult Success(
        IReadOnlyList<EmployeeSalaryHistoryInfo> salaryHistory)
    {
        ArgumentNullException.ThrowIfNull(salaryHistory);

        return new EmployeeSalaryHistoryResult(
            IsSuccessful: true,
            ErrorCode: EmployeeErrorCode.None,
            SalaryHistory: salaryHistory);
    }

    public static EmployeeSalaryHistoryResult Failure(
        EmployeeErrorCode errorCode)
    {
        if (errorCode == EmployeeErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new EmployeeSalaryHistoryResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            SalaryHistory: []);
    }
}
