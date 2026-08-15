namespace LithoManager.Application.Features.Documents;

public sealed record EmployeeRecordResult(
    bool IsSuccessful,
    DocumentErrorCode ErrorCode,
    EmployeeRecordInfo? EmployeeRecord)
{
    public static EmployeeRecordResult Success(
        EmployeeRecordInfo employeeRecord)
    {
        ArgumentNullException.ThrowIfNull(employeeRecord);

        return new EmployeeRecordResult(
            IsSuccessful: true,
            ErrorCode: DocumentErrorCode.None,
            EmployeeRecord: employeeRecord);
    }

    public static EmployeeRecordResult Failure(
        DocumentErrorCode errorCode)
    {
        if (errorCode == DocumentErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new EmployeeRecordResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            EmployeeRecord: null);
    }
}
