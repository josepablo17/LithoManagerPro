namespace LithoManager.Application.Features
    .HumanResources.Employees;

public sealed record EmployeeIdentificationTypesResult(
    bool IsSuccessful,
    EmployeeErrorCode ErrorCode,
    IReadOnlyList<EmployeeIdentificationTypeInfo>
        IdentificationTypes)
{
    public static EmployeeIdentificationTypesResult Success(
        IReadOnlyList<EmployeeIdentificationTypeInfo>
            identificationTypes)
    {
        ArgumentNullException.ThrowIfNull(
            identificationTypes);

        return new EmployeeIdentificationTypesResult(
            IsSuccessful: true,
            ErrorCode: EmployeeErrorCode.None,
            IdentificationTypes: identificationTypes);
    }

    public static EmployeeIdentificationTypesResult Failure(
        EmployeeErrorCode errorCode)
    {
        if (errorCode == EmployeeErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new EmployeeIdentificationTypesResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            IdentificationTypes: []);
    }
}
