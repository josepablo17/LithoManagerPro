namespace LithoManager.Application.Features.Payroll;

public sealed record PayrollItemsResult<TItem>(
    bool IsSuccessful,
    PayrollErrorCode ErrorCode,
    IReadOnlyList<TItem> Items)
{
    public static PayrollItemsResult<TItem> Success(
        IReadOnlyList<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return new PayrollItemsResult<TItem>(
            IsSuccessful: true,
            ErrorCode: PayrollErrorCode.None,
            Items: items);
    }

    public static PayrollItemsResult<TItem> Failure(
        PayrollErrorCode errorCode)
    {
        if (errorCode == PayrollErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new PayrollItemsResult<TItem>(
            IsSuccessful: false,
            ErrorCode: errorCode,
            Items: []);
    }
}

public sealed record EmployeeWorkScheduleResult(
    bool IsSuccessful,
    PayrollErrorCode ErrorCode,
    EmployeeWorkScheduleInfo? EmployeeWorkSchedule)
{
    public static EmployeeWorkScheduleResult Success(
        EmployeeWorkScheduleInfo employeeWorkSchedule)
    {
        ArgumentNullException.ThrowIfNull(employeeWorkSchedule);

        return new EmployeeWorkScheduleResult(
            true,
            PayrollErrorCode.None,
            employeeWorkSchedule);
    }

    public static EmployeeWorkScheduleResult Failure(
        PayrollErrorCode errorCode)
    {
        return new EmployeeWorkScheduleResult(
            false,
            errorCode,
            null);
    }
}

public sealed record AttendanceRecordResult(
    bool IsSuccessful,
    PayrollErrorCode ErrorCode,
    AttendanceRecordInfo? AttendanceRecord)
{
    public static AttendanceRecordResult Success(
        AttendanceRecordInfo attendanceRecord)
    {
        ArgumentNullException.ThrowIfNull(attendanceRecord);

        return new AttendanceRecordResult(
            true,
            PayrollErrorCode.None,
            attendanceRecord);
    }

    public static AttendanceRecordResult Failure(
        PayrollErrorCode errorCode)
    {
        return new AttendanceRecordResult(
            false,
            errorCode,
            null);
    }
}

public sealed record OvertimeRecordResult(
    bool IsSuccessful,
    PayrollErrorCode ErrorCode,
    OvertimeRecordInfo? OvertimeRecord)
{
    public static OvertimeRecordResult Success(
        OvertimeRecordInfo overtimeRecord)
    {
        ArgumentNullException.ThrowIfNull(overtimeRecord);

        return new OvertimeRecordResult(
            true,
            PayrollErrorCode.None,
            overtimeRecord);
    }

    public static OvertimeRecordResult Failure(
        PayrollErrorCode errorCode)
    {
        return new OvertimeRecordResult(
            false,
            errorCode,
            null);
    }
}

public sealed record EmployeeDisabilityResult(
    bool IsSuccessful,
    PayrollErrorCode ErrorCode,
    EmployeeDisabilityInfo? EmployeeDisability)
{
    public static EmployeeDisabilityResult Success(
        EmployeeDisabilityInfo employeeDisability)
    {
        ArgumentNullException.ThrowIfNull(employeeDisability);

        return new EmployeeDisabilityResult(
            true,
            PayrollErrorCode.None,
            employeeDisability);
    }

    public static EmployeeDisabilityResult Failure(
        PayrollErrorCode errorCode)
    {
        return new EmployeeDisabilityResult(
            false,
            errorCode,
            null);
    }
}
