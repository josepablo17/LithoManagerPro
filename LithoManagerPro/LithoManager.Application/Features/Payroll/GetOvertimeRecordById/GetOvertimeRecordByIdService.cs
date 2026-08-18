using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.GetOvertimeRecordById;

public sealed class GetOvertimeRecordByIdService
    : IGetOvertimeRecordByIdService
{
    private readonly IPayrollRepository _payrollRepository;

    public GetOvertimeRecordByIdService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<OvertimeRecordResult> GetAsync(
        GetOvertimeRecordByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!PayrollValidation.IsValidPositiveId(
                query.OvertimeRecordId)
            || !PayrollValidation.IsValidPositiveId(
                query.ActorUserId))
        {
            return OvertimeRecordResult.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        try
        {
            OvertimeRecordData? overtimeRecord =
                await _payrollRepository.GetOvertimeRecordByIdAsync(
                    query.OvertimeRecordId,
                    query.ActorUserId,
                    cancellationToken);

            return overtimeRecord is null
                ? OvertimeRecordResult.Failure(
                    PayrollErrorCode.OvertimeRecordNotFound)
                : OvertimeRecordResult.Success(
                    PayrollMapper.Map(overtimeRecord));
        }
        catch (PayrollPersistenceException exception)
        {
            return OvertimeRecordResult.Failure(
                exception.ErrorCode);
        }
    }
}
