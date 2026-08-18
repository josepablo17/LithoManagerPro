using LithoManager.Application.Abstractions.Persistence;

namespace LithoManager.Application.Features
    .Payroll.GetPayrollConfiguration;

public sealed class PayrollConfigurationService
    : IPayrollConfigurationService
{
    private readonly IPayrollRepository _payrollRepository;

    public PayrollConfigurationService(
        IPayrollRepository payrollRepository)
    {
        ArgumentNullException.ThrowIfNull(payrollRepository);

        _payrollRepository = payrollRepository;
    }

    public async Task<PayrollItemsResult<PayrollConceptInfo>>
        GetPayrollConceptsAsync(
            ActivePayrollConfigurationQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            IReadOnlyList<PayrollConceptData> concepts =
                await _payrollRepository
                    .GetPayrollConceptsAsync(
                        query.IsActive,
                        cancellationToken);

            return PayrollItemsResult<PayrollConceptInfo>
                .Success(concepts.Select(PayrollMapper.Map).ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<PayrollConceptInfo>
                .Failure(exception.ErrorCode);
        }
    }

    public async Task<
            PayrollItemsResult<SocialContributionTypeInfo>>
        GetSocialContributionTypesAsync(
            ActivePayrollConfigurationQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            IReadOnlyList<SocialContributionTypeData> types =
                await _payrollRepository
                    .GetSocialContributionTypesAsync(
                        query.IsActive,
                        cancellationToken);

            return PayrollItemsResult<SocialContributionTypeInfo>
                .Success(types.Select(PayrollMapper.Map).ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<SocialContributionTypeInfo>
                .Failure(exception.ErrorCode);
        }
    }

    public async Task<
            PayrollItemsResult<SocialContributionRateInfo>>
        GetSocialContributionRatesAsync(
            EffectivePayrollConfigurationQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            IReadOnlyList<SocialContributionRateData> rates =
                await _payrollRepository
                    .GetSocialContributionRatesAsync(
                        query.AsOfDate,
                        query.IsActive,
                        cancellationToken);

            return PayrollItemsResult<SocialContributionRateInfo>
                .Success(rates.Select(PayrollMapper.Map).ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<SocialContributionRateInfo>
                .Failure(exception.ErrorCode);
        }
    }

    public async Task<PayrollItemsResult<
            SocialContributionMinimumBaseInfo>>
        GetSocialContributionMinimumBasesAsync(
            EffectivePayrollConfigurationQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            IReadOnlyList<SocialContributionMinimumBaseData>
                minimumBases =
                    await _payrollRepository
                        .GetSocialContributionMinimumBasesAsync(
                            query.AsOfDate,
                            query.IsActive,
                            cancellationToken);

            return PayrollItemsResult<
                    SocialContributionMinimumBaseInfo>
                .Success(
                    minimumBases.Select(PayrollMapper.Map)
                        .ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<
                    SocialContributionMinimumBaseInfo>
                .Failure(exception.ErrorCode);
        }
    }

    public async Task<PayrollItemsResult<IncomeTaxBracketInfo>>
        GetIncomeTaxBracketsAsync(
            IncomeTaxConfigurationQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!PayrollValidation.IsValidTaxYear(query.TaxYear)
            || !PayrollValidation.IsValidPeriodicity(
                query.Periodicity))
        {
            return PayrollItemsResult<IncomeTaxBracketInfo>
                .Failure(PayrollErrorCode.InvalidRequest);
        }

        try
        {
            IReadOnlyList<IncomeTaxBracketData> brackets =
                await _payrollRepository
                    .GetIncomeTaxBracketsAsync(
                        query.TaxYear,
                        PayrollValidation.NormalizePeriodicity(
                            query.Periodicity),
                        query.AsOfDate,
                        cancellationToken);

            return PayrollItemsResult<IncomeTaxBracketInfo>
                .Success(brackets.Select(PayrollMapper.Map).ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<IncomeTaxBracketInfo>
                .Failure(exception.ErrorCode);
        }
    }

    public async Task<PayrollItemsResult<IncomeTaxCreditInfo>>
        GetIncomeTaxCreditsAsync(
            IncomeTaxConfigurationQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!PayrollValidation.IsValidTaxYear(query.TaxYear)
            || !PayrollValidation.IsValidPeriodicity(
                query.Periodicity))
        {
            return PayrollItemsResult<IncomeTaxCreditInfo>
                .Failure(PayrollErrorCode.InvalidRequest);
        }

        try
        {
            IReadOnlyList<IncomeTaxCreditData> credits =
                await _payrollRepository
                    .GetIncomeTaxCreditsAsync(
                        query.TaxYear,
                        PayrollValidation.NormalizePeriodicity(
                            query.Periodicity),
                        query.AsOfDate,
                        query.IsActive,
                        cancellationToken);

            return PayrollItemsResult<IncomeTaxCreditInfo>
                .Success(credits.Select(PayrollMapper.Map).ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<IncomeTaxCreditInfo>
                .Failure(exception.ErrorCode);
        }
    }

    public async Task<PayrollItemsResult<WorkShiftTypeInfo>>
        GetWorkShiftTypesAsync(
            EffectivePayrollConfigurationQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            IReadOnlyList<WorkShiftTypeData> workShiftTypes =
                await _payrollRepository
                    .GetWorkShiftTypesAsync(
                        query.AsOfDate,
                        query.IsActive,
                        cancellationToken);

            return PayrollItemsResult<WorkShiftTypeInfo>
                .Success(
                    workShiftTypes.Select(PayrollMapper.Map)
                        .ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<WorkShiftTypeInfo>
                .Failure(exception.ErrorCode);
        }
    }

    public async Task<PayrollItemsResult<OvertimeRuleInfo>>
        GetOvertimeRulesAsync(
            EffectivePayrollConfigurationQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            IReadOnlyList<OvertimeRuleData> overtimeRules =
                await _payrollRepository
                    .GetOvertimeRulesAsync(
                        query.AsOfDate,
                        query.IsActive,
                        cancellationToken);

            return PayrollItemsResult<OvertimeRuleInfo>
                .Success(
                    overtimeRules.Select(PayrollMapper.Map)
                        .ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<OvertimeRuleInfo>
                .Failure(exception.ErrorCode);
        }
    }

    public async Task<PayrollItemsResult<DisabilityTypeInfo>>
        GetDisabilityTypesAsync(
            ActivePayrollConfigurationQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            IReadOnlyList<DisabilityTypeData> disabilityTypes =
                await _payrollRepository
                    .GetDisabilityTypesAsync(
                        query.IsActive,
                        cancellationToken);

            return PayrollItemsResult<DisabilityTypeInfo>
                .Success(
                    disabilityTypes.Select(PayrollMapper.Map)
                        .ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<DisabilityTypeInfo>
                .Failure(exception.ErrorCode);
        }
    }

    public async Task<PayrollItemsResult<AguinaldoRuleInfo>>
        GetAguinaldoRulesAsync(
            EffectivePayrollConfigurationQuery query,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            IReadOnlyList<AguinaldoRuleData> aguinaldoRules =
                await _payrollRepository
                    .GetAguinaldoRulesAsync(
                        query.AsOfDate,
                        query.IsActive,
                        cancellationToken);

            return PayrollItemsResult<AguinaldoRuleInfo>
                .Success(
                    aguinaldoRules.Select(PayrollMapper.Map)
                        .ToArray());
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollItemsResult<AguinaldoRuleInfo>
                .Failure(exception.ErrorCode);
        }
    }
}
