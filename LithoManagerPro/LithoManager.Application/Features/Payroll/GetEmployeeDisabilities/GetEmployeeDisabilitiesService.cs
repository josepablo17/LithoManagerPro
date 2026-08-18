using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.GetEmployeeDisabilities;

public sealed class GetEmployeeDisabilitiesService
    : IGetEmployeeDisabilitiesService
{
    private readonly IPayrollRepository _payrollRepository;

    public GetEmployeeDisabilitiesService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<PayrollItemsResult<EmployeeDisabilityInfo>>
        GetAsync(
            GetEmployeeDisabilitiesQuery query,
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
                query.DisabilityTypeId)
            || !PayrollValidation.IsValidOptionalDisabilityStatus(
                query.DisabilityStatus)
            || !PayrollValidation.IsValidOptionalIssuerInstitution(
                query.IssuerInstitution)
            || !PayrollValidation.IsValidOptionalDateRange(
                query.DateFrom,
                query.DateTo)
            || !PayrollValidation.IsValidSearchTerm(
                query.SearchTerm))
        {
            return PayrollItemsResult<EmployeeDisabilityInfo>
                .Failure(PayrollErrorCode.InvalidRequest);
        }

        try
        {
            IReadOnlyList<EmployeeDisabilityData> disabilities =
                await _payrollRepository.GetEmployeeDisabilitiesAsync(
                    query.ActorUserId,
                    query.EmployeeId,
                    query.DepartmentId,
                    query.DisabilityTypeId,
                    PayrollValidation.NormalizeOptionalText(
                        query.DisabilityStatus),
                    PayrollValidation.NormalizeOptionalText(
                        query.IssuerInstitution),
                    query.DateFrom?.Date,
                    query.DateTo?.Date,
                    PayrollValidation.NormalizeOptionalText(
                        query.SearchTerm),
                    cancellationToken);

            return PayrollItemsResult<EmployeeDisabilityInfo>.Success(
                disabilities.Select(PayrollMapper.Map)
                    .ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<EmployeeDisabilityInfo>
                .Failure(exception.ErrorCode);
        }
    }
}
