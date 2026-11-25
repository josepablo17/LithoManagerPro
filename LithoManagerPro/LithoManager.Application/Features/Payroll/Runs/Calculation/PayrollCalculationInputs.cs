namespace LithoManager.Application.Features.Payroll.Runs.Calculation;

/// <summary>Inputs for a regular (monthly) payroll calculation.</summary>
public sealed class PayrollCalculationInputs
{
    public DateTime PeriodStartDate { get; init; }
    public DateTime PeriodEndDate { get; init; }
    public IReadOnlyList<CalculationEmployee> Employees { get; init; } = [];
    public IReadOnlyList<SalarySegment> SalarySegments { get; init; } = [];
    public IReadOnlyList<WorkSchedule> WorkSchedules { get; init; } = [];
    public IReadOnlyList<ApprovedOvertime> Overtime { get; init; } = [];
    public IReadOnlyList<ApprovedDisability> Disabilities { get; init; } = [];
    public IReadOnlyList<CalculationConcept> Concepts { get; init; } = [];
    public IReadOnlyList<ContributionRate> ContributionRates { get; init; } = [];
    public IReadOnlyList<ContributionMinimumBase> MinimumBases { get; init; } = [];
    public IReadOnlyList<IncomeTaxBracket> IncomeTaxBrackets { get; init; } = [];
}

public sealed class CalculationEmployee
{
    public int EmployeeId { get; init; }
    public string IdentificationNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? DepartmentName { get; init; }
    public string? JobTitle { get; init; }
    public DateTime HireDate { get; init; }
    public DateTime? TerminationDate { get; init; }

    /// <summary>Current salary, used when no history segment covers a day.</summary>
    public decimal BaseSalary { get; init; }
}

public sealed class SalarySegment
{
    public int EmployeeId { get; init; }
    public decimal BaseSalary { get; init; }
    public DateTime EffectiveFromDate { get; init; }
    public DateTime? EffectiveToDate { get; init; }
}

public sealed class WorkSchedule
{
    public int EmployeeId { get; init; }
    public string WorkShiftTypeCode { get; init; } = string.Empty;
    public decimal MaxOrdinaryHoursPerDay { get; init; }
    public DateTime EffectiveFromDate { get; init; }
    public DateTime? EffectiveToDate { get; init; }
}

public sealed class ApprovedOvertime
{
    public int OvertimeRecordId { get; init; }
    public int EmployeeId { get; init; }
    public DateTime OvertimeDate { get; init; }
    public decimal Hours { get; init; }
    public string OvertimeRuleCode { get; init; } = string.Empty;
    public string OvertimeRuleName { get; init; } = string.Empty;
    public decimal HourMultiplier { get; init; }
}

public sealed class ApprovedDisability
{
    public int EmployeeDisabilityId { get; init; }
    public int EmployeeId { get; init; }
    public string DisabilityTypeCode { get; init; } = string.Empty;
    public string DisabilityTypeName { get; init; } = string.Empty;
    public bool ReducesWorkedDays { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal? EmployerPaidAmount { get; init; }
}

public sealed class CalculationConcept
{
    public int PayrollConceptId { get; init; }
    public string PayrollConceptCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ConceptKind { get; init; } = string.Empty;
    public bool IsTaxableForIncomeTax { get; init; }
    public bool IsSubjectToSocialContributions { get; init; }
    public bool CountsForAguinaldo { get; init; }
}

public sealed class ContributionRate
{
    public string ContributionCode { get; init; } = string.Empty;
    public string ContributionName { get; init; } = string.Empty;
    public bool AppliesToEmployee { get; init; }
    public bool AppliesToEmployer { get; init; }
    public bool UsesMinimumBase { get; init; }
    public decimal EmployeeRate { get; init; }
    public decimal EmployerRate { get; init; }
}

public sealed class ContributionMinimumBase
{
    public string ContributionCode { get; init; } = string.Empty;
    public decimal MinimumBaseAmount { get; init; }
}

public sealed class IncomeTaxBracket
{
    public decimal LowerBoundAmount { get; init; }
    public decimal? UpperBoundAmount { get; init; }
    public decimal TaxRate { get; init; }
}

/// <summary>Inputs for the yearly aguinaldo calculation.</summary>
public sealed class AguinaldoCalculationInputs
{
    public DateTime WindowStartDate { get; init; }
    public DateTime WindowEndDate { get; init; }
    public IReadOnlyList<AguinaldoEmployee> Employees { get; init; } = [];
    public IReadOnlyList<AguinaldoSourceLine> SourceLines { get; init; } = [];
    public IReadOnlyList<ClosedPayrollPeriod> ClosedPeriods { get; init; } = [];
    public AguinaldoRule? Rule { get; init; }
    public IReadOnlyList<DisabilityTypeFlag> DisabilityTypes { get; init; } = [];
    public IReadOnlyList<CalculationConcept> Concepts { get; init; } = [];
}

public sealed class AguinaldoEmployee
{
    public int EmployeeId { get; init; }
    public string IdentificationNumber { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public string? DepartmentName { get; init; }
    public string? JobTitle { get; init; }
    public decimal MonthlyBaseSalary { get; init; }
}

public sealed class AguinaldoSourceLine
{
    public int EmployeeId { get; init; }
    public int PeriodYear { get; init; }
    public int PeriodMonth { get; init; }
    public string PayrollConceptCode { get; init; } = string.Empty;
    public string? ReferenceCode { get; init; }
    public decimal Amount { get; init; }
}

public sealed class ClosedPayrollPeriod
{
    public int PeriodYear { get; init; }
    public int PeriodMonth { get; init; }
}

public sealed class AguinaldoRule
{
    public string AguinaldoRuleCode { get; init; } = string.Empty;
    public int Divisor { get; init; }
    public bool IncludesOrdinarySalary { get; init; }
    public bool IncludesOvertime { get; init; }
    public bool IncludesSalaryInKind { get; init; }
    public bool ExcludesCommonIllnessSubsidy { get; init; }
    public bool IncludesMaternitySubsidy { get; init; }
}

public sealed class DisabilityTypeFlag
{
    public string DisabilityTypeCode { get; init; } = string.Empty;
    public bool CountsAsSalaryForAguinaldo { get; init; }
}
