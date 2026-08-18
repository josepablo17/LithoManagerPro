using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.GetAttendanceRecordById;

public sealed class GetAttendanceRecordByIdService
    : IGetAttendanceRecordByIdService
{
    private readonly IPayrollRepository _payrollRepository;

    public GetAttendanceRecordByIdService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<AttendanceRecordResult> GetAsync(
        GetAttendanceRecordByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!PayrollValidation.IsValidPositiveId(
                query.AttendanceRecordId)
            || !PayrollValidation.IsValidPositiveId(
                query.ActorUserId))
        {
            return AttendanceRecordResult.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        try
        {
            AttendanceRecordData? attendanceRecord =
                await _payrollRepository.GetAttendanceRecordByIdAsync(
                    query.AttendanceRecordId,
                    query.ActorUserId,
                    cancellationToken);

            return attendanceRecord is null
                ? AttendanceRecordResult.Failure(
                    PayrollErrorCode.AttendanceRecordNotFound)
                : AttendanceRecordResult.Success(
                    PayrollMapper.Map(attendanceRecord));
        }
        catch (PayrollPersistenceException exception)
        {
            return AttendanceRecordResult.Failure(
                exception.ErrorCode);
        }
    }
}
