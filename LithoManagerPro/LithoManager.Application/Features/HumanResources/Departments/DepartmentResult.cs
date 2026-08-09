namespace LithoManager.Application.Features
    .HumanResources.Departments;

public sealed record DepartmentResult(
    bool IsSuccessful,
    DepartmentErrorCode ErrorCode,
    DepartmentInfo? Department)
{
    public static DepartmentResult Success(
        DepartmentInfo department)
    {
        ArgumentNullException.ThrowIfNull(department);

        return new DepartmentResult(
            IsSuccessful: true,
            ErrorCode: DepartmentErrorCode.None,
            Department: department);
    }

    public static DepartmentResult Failure(
        DepartmentErrorCode errorCode)
    {
        if (errorCode == DepartmentErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new DepartmentResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            Department: null);
    }
}
