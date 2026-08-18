using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.GetOvertimeRecords;

public sealed class GetOvertimeRecordsService
    : IGetOvertimeRecordsService
{
    private readonly IPayrollRepository _payrollRepository;

    public GetOvertimeRecordsService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<PayrollItemsResult<OvertimeRecordInfo>>
        GetAsync(
            GetOvertimeRecordsQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!PayrollValidation.IsValidPositiveId(
                query.ActorUserId)
            || !PayrollValidation.IsValidOptionalPositiveId(
                query.EmployeeId)
            || !PayrollValidation.IsValidOptionalPositiveId(
                query.DepartmentId)
            || !PayrollValidation.IsValidOptionalPositiveId(
                query.OvertimeRuleId)
            || !PayrollValidation
                .IsValidOptionalOvertimeApprovalStatus(
                    query.ApprovalStatus)
            || !PayrollValidation.IsValidOptionalDateRange(
                query.DateFrom,
                query.DateTo)
            || !PayrollValidation.IsValidSearchTerm(
                query.SearchTerm))
        {
            return PayrollItemsResult<OvertimeRecordInfo>
                .Failure(PayrollErrorCode.InvalidRequest);
        }

        try
        {
            IReadOnlyList<OvertimeRecordData> overtimeRecords =
                await _payrollRepository.GetOvertimeRecordsAsync(
                    query.ActorUserId,
                    query.EmployeeId,
                    query.DepartmentId,
                    query.OvertimeRuleId,
                    PayrollValidation.NormalizeOptionalText(
                        query.ApprovalStatus),
                    query.DateFrom?.Date,
                    query.DateTo?.Date,
                    PayrollValidation.NormalizeOptionalText(
                        query.SearchTerm),
                    cancellationToken);

            return PayrollItemsResult<OvertimeRecordInfo>.Success(
                overtimeRecords.Select(PayrollMapper.Map)
                    .ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<OvertimeRecordInfo>
                .Failure(exception.ErrorCode);
        }
    }
}
