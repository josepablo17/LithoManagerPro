namespace LithoManager.Application.Features
    .HumanResources.Departments;

public sealed record DepartmentsResult(
    bool IsSuccessful,
    DepartmentErrorCode ErrorCode,
    IReadOnlyList<DepartmentInfo> Departments)
{
    public static DepartmentsResult Success(
        IReadOnlyList<DepartmentInfo> departments)
    {
        ArgumentNullException.ThrowIfNull(departments);

        return new DepartmentsResult(
            IsSuccessful: true,
            ErrorCode: DepartmentErrorCode.None,
            Departments: departments);
    }

    public static DepartmentsResult Failure(
        DepartmentErrorCode errorCode)
    {
        if (errorCode == DepartmentErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new DepartmentsResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            Departments: []);
    }
}
