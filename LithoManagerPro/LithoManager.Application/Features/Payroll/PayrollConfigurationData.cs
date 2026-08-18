namespace LithoManager.Application.Features.Payroll;

public sealed class PayrollConceptData
{
    public int PayrollConceptId { get; init; }
    public string PayrollConceptCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ConceptKind { get; init; } = string.Empty;
    public bool IsSystemConcept { get; init; }
    public bool IsTaxableForIncomeTax { get; init; }
    public bool IsSubjectToSocialContributions { get; init; }
    public bool CountsForAguinaldo { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class SocialContributionTypeData
{
    public int SocialContributionTypeId { get; init; }
    public string ContributionCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string InstitutionName { get; init; } = string.Empty;
    public string ContributionGroup { get; init; } = string.Empty;
    public bool AppliesToEmployee { get; init; }
    public bool AppliesToEmployer { get; init; }
    public bool UsesMinimumBase { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class SocialContributionRateData
{
    public int SocialContributionRateId { get; init; }
    public int SocialContributionTypeId { get; init; }
    public string ContributionCode { get; init; } = string.Empty;
    public string ContributionName { get; init; } = string.Empty;
    public string InstitutionName { get; init; } = string.Empty;
    public string ContributionGroup { get; init; } = string.Empty;
    public bool AppliesToEmployee { get; init; }
    public bool AppliesToEmployer { get; init; }
    public bool UsesMinimumBase { get; init; }
    public decimal EmployeeRate { get; init; }
    public decimal EmployerRate { get; init; }
    public DateTime EffectiveFromDate { get; init; }
    public DateTime? EffectiveToDate { get; init; }
    public string? LegalReference { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class SocialContributionMinimumBaseData
{
    public int SocialContributionMinimumBaseId { get; init; }
    public int SocialContributionTypeId { get; init; }
    public string ContributionCode { get; init; } = string.Empty;
    public string ContributionName { get; init; } = string.Empty;
    public string InstitutionName { get; init; } = string.Empty;
    public string ContributionGroup { get; init; } = string.Empty;
    public decimal MinimumBaseAmount { get; init; }
    public DateTime EffectiveFromDate { get; init; }
    public DateTime? EffectiveToDate { get; init; }
    public string? LegalReference { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class IncomeTaxBracketData
{
    public int IncomeTaxBracketId { get; init; }
    public int TaxYear { get; init; }
    public string Periodicity { get; init; } = string.Empty;
    public decimal LowerBoundAmount { get; init; }
    public decimal? UpperBoundAmount { get; init; }
    public decimal TaxRate { get; init; }
    public DateTime EffectiveFromDate { get; init; }
    public DateTime? EffectiveToDate { get; init; }
    public string? LegalReference { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class IncomeTaxCreditData
{
    public int IncomeTaxCreditId { get; init; }
    public string CreditCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int TaxYear { get; init; }
    public string Periodicity { get; init; } = string.Empty;
    public decimal CreditAmount { get; init; }
    public DateTime EffectiveFromDate { get; init; }
    public DateTime? EffectiveToDate { get; init; }
    public string? LegalReference { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class WorkShiftTypeData
{
    public int WorkShiftTypeId { get; init; }
    public string WorkShiftTypeCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal MaxOrdinaryHoursPerDay { get; init; }
    public decimal MaxOrdinaryHoursPerWeek { get; init; }
    public decimal MaxTotalHoursPerDay { get; init; }
    public DateTime EffectiveFromDate { get; init; }
    public DateTime? EffectiveToDate { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class OvertimeRuleData
{
    public int OvertimeRuleId { get; init; }
    public string OvertimeRuleCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal HourMultiplier { get; init; }
    public bool CountsForAguinaldo { get; init; }
    public DateTime EffectiveFromDate { get; init; }
    public DateTime? EffectiveToDate { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class DisabilityTypeData
{
    public int DisabilityTypeId { get; init; }
    public string DisabilityTypeCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool CountsAsSalaryForAguinaldo { get; init; }
    public bool RequiresSubsidyTracking { get; init; }
    public bool ReducesWorkedDays { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class AguinaldoRuleData
{
    public int AguinaldoRuleId { get; init; }
    public string AguinaldoRuleCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public byte CalculationStartMonth { get; init; }
    public byte CalculationStartDay { get; init; }
    public byte CalculationEndMonth { get; init; }
    public byte CalculationEndDay { get; init; }
    public decimal Divisor { get; init; }
    public byte PaymentDueMonth { get; init; }
    public byte PaymentDueDay { get; init; }
    public bool IncludesOrdinarySalary { get; init; }
    public bool IncludesOvertime { get; init; }
    public bool IncludesSalaryInKind { get; init; }
    public bool ExcludesCommonIllnessSubsidy { get; init; }
    public bool IncludesMaternitySubsidy { get; init; }
    public DateTime EffectiveFromDate { get; init; }
    public DateTime? EffectiveToDate { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}
