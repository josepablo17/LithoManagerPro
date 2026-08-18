namespace LithoManager.Application.Features
    .Payroll.GetPayrollConfiguration;

public interface IPayrollConfigurationService
{
    Task<PayrollItemsResult<PayrollConceptInfo>>
        GetPayrollConceptsAsync(
            ActivePayrollConfigurationQuery query,
            CancellationToken cancellationToken);

    Task<PayrollItemsResult<SocialContributionTypeInfo>>
        GetSocialContributionTypesAsync(
            ActivePayrollConfigurationQuery query,
            CancellationToken cancellationToken);

    Task<PayrollItemsResult<SocialContributionRateInfo>>
        GetSocialContributionRatesAsync(
            EffectivePayrollConfigurationQuery query,
            CancellationToken cancellationToken);

    Task<PayrollItemsResult<SocialContributionMinimumBaseInfo>>
        GetSocialContributionMinimumBasesAsync(
            EffectivePayrollConfigurationQuery query,
            CancellationToken cancellationToken);

    Task<PayrollItemsResult<IncomeTaxBracketInfo>>
        GetIncomeTaxBracketsAsync(
            IncomeTaxConfigurationQuery query,
            CancellationToken cancellationToken);

    Task<PayrollItemsResult<IncomeTaxCreditInfo>>
        GetIncomeTaxCreditsAsync(
            IncomeTaxConfigurationQuery query,
            CancellationToken cancellationToken);

    Task<PayrollItemsResult<WorkShiftTypeInfo>>
        GetWorkShiftTypesAsync(
            EffectivePayrollConfigurationQuery query,
            CancellationToken cancellationToken);

    Task<PayrollItemsResult<OvertimeRuleInfo>>
        GetOvertimeRulesAsync(
            EffectivePayrollConfigurationQuery query,
            CancellationToken cancellationToken);

    Task<PayrollItemsResult<DisabilityTypeInfo>>
        GetDisabilityTypesAsync(
            ActivePayrollConfigurationQuery query,
            CancellationToken cancellationToken);

    Task<PayrollItemsResult<AguinaldoRuleInfo>>
        GetAguinaldoRulesAsync(
            EffectivePayrollConfigurationQuery query,
            CancellationToken cancellationToken);
}
