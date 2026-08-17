namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed record AssignableEmployeeUsersResult(
    bool IsSuccessful,
    EmployeeErrorCode ErrorCode,
    IReadOnlyList<AssignableEmployeeUserInfo> Users)
{
    public static AssignableEmployeeUsersResult Success(
        IReadOnlyList<AssignableEmployeeUserInfo> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        return new AssignableEmployeeUsersResult(
            IsSuccessful: true,
            ErrorCode: EmployeeErrorCode.None,
            Users: users);
    }

    public static AssignableEmployeeUsersResult Failure(
        EmployeeErrorCode errorCode)
    {
        if (errorCode == EmployeeErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new AssignableEmployeeUsersResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            Users: []);
    }
}
