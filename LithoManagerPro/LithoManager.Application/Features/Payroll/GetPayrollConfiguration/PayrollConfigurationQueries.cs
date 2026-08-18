namespace LithoManager.Application.Features
    .Payroll.GetPayrollConfiguration;

public sealed record EffectivePayrollConfigurationQuery(
    DateTime? AsOfDate,
    bool? IsActive);

public sealed record IncomeTaxConfigurationQuery(
    int TaxYear,
    string? Periodicity,
    DateTime? AsOfDate,
    bool? IsActive);

public sealed record ActivePayrollConfigurationQuery(
    bool? IsActive);
