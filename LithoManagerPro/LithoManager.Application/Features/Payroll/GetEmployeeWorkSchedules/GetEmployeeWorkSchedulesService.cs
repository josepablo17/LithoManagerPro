using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.GetEmployeeWorkSchedules;

public sealed record GetEmployeeWorkSchedulesQuery(
    int? EmployeeId,
    bool IncludeHistory);

public interface IGetEmployeeWorkSchedulesService
{
    Task<PayrollItemsResult<EmployeeWorkScheduleInfo>> GetAsync(
        GetEmployeeWorkSchedulesQuery query,
        CancellationToken cancellationToken);
}

public sealed class GetEmployeeWorkSchedulesService
    : IGetEmployeeWorkSchedulesService
{
    private readonly IPayrollRepository _payrollRepository;

    public GetEmployeeWorkSchedulesService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<PayrollItemsResult<EmployeeWorkScheduleInfo>>
        GetAsync(
            GetEmployeeWorkSchedulesQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!PayrollValidation.IsValidOptionalPositiveId(
                query.EmployeeId))
        {
            return PayrollItemsResult<EmployeeWorkScheduleInfo>
                .Failure(PayrollErrorCode.InvalidRequest);
        }

        IReadOnlyList<EmployeeWorkScheduleData> schedules =
            await _payrollRepository.GetEmployeeWorkSchedulesAsync(
                query.EmployeeId,
                query.IncludeHistory,
                cancellationToken);

        return PayrollItemsResult<EmployeeWorkScheduleInfo>.Success(
            schedules.Select(PayrollMapper.Map).ToArray());
    }
}
