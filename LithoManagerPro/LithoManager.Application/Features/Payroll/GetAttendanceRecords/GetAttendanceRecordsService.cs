using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.GetAttendanceRecords;

public sealed class GetAttendanceRecordsService
    : IGetAttendanceRecordsService
{
    private readonly IPayrollRepository _payrollRepository;

    public GetAttendanceRecordsService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<PayrollItemsResult<AttendanceRecordInfo>>
        GetAsync(
            GetAttendanceRecordsQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!PayrollValidation.IsValidPositiveId(
                query.ActorUserId)
            || !PayrollValidation.IsValidOptionalPositiveId(
                query.EmployeeId)
            || !PayrollValidation.IsValidOptionalPositiveId(
                query.DepartmentId)
            || !PayrollValidation.IsValidOptionalAttendanceStatus(
                query.AttendanceStatus)
            || !PayrollValidation.IsValidOptionalDateRange(
                query.DateFrom,
                query.DateTo)
            || !PayrollValidation.IsValidSearchTerm(
                query.SearchTerm))
        {
            return PayrollItemsResult<AttendanceRecordInfo>
                .Failure(PayrollErrorCode.InvalidRequest);
        }

        try
        {
            IReadOnlyList<AttendanceRecordData> attendanceRecords =
                await _payrollRepository.GetAttendanceRecordsAsync(
                    query.ActorUserId,
                    query.EmployeeId,
                    query.DepartmentId,
                    PayrollValidation.NormalizeOptionalText(
                        query.AttendanceStatus),
                    query.IsApproved,
                    query.DateFrom?.Date,
                    query.DateTo?.Date,
                    PayrollValidation.NormalizeOptionalText(
                        query.SearchTerm),
                    cancellationToken);

            return PayrollItemsResult<AttendanceRecordInfo>.Success(
                attendanceRecords.Select(PayrollMapper.Map)
                    .ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<AttendanceRecordInfo>
                .Failure(exception.ErrorCode);
        }
    }
}
