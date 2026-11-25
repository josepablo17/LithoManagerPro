using LithoManager.Application.Features.Payroll.Runs.Calculation;

namespace LithoManager.UnitTests.Features.Payroll.Runs;

/// <summary>
/// Configuration matching the 2026 Costa Rica seed data
/// (Seeds/Payroll/LegalConfiguration.sql).
/// </summary>
internal static class PayrollTestData
{
    public static readonly IReadOnlyList<CalculationConcept> Concepts =
    [
        Concept(1, PayrollConceptCodes.BaseSalary, PayrollConceptKinds.Earning, taxable: true, subject: true, aguinaldo: true),
        Concept(2, PayrollConceptCodes.Overtime, PayrollConceptKinds.Earning, taxable: true, subject: true, aguinaldo: true),
        Concept(3, PayrollConceptCodes.SalaryInKind, PayrollConceptKinds.Earning, taxable: true, subject: true, aguinaldo: true),
        Concept(4, PayrollConceptCodes.Aguinaldo, PayrollConceptKinds.Earning, taxable: false, subject: false, aguinaldo: false),
        Concept(5, PayrollConceptCodes.EmployeeSocialContribution, PayrollConceptKinds.Deduction, taxable: false, subject: false, aguinaldo: false),
        Concept(6, PayrollConceptCodes.IncomeTax, PayrollConceptKinds.Deduction, taxable: false, subject: false, aguinaldo: false),
        Concept(7, PayrollConceptCodes.EmployerSocialContribution, PayrollConceptKinds.EmployerContribution, taxable: false, subject: false, aguinaldo: false),
        Concept(8, PayrollConceptCodes.DisabilitySubsidy, PayrollConceptKinds.Informational, taxable: false, subject: false, aguinaldo: false),
    ];

    public static readonly IReadOnlyList<ContributionRate> ContributionRates =
    [
        Rate("SEM", employee: 0.055m, employer: 0.0925m, usesMinimumBase: true),
        Rate("IVM", employee: 0.0433m, employer: 0.0558m, usesMinimumBase: true),
        Rate("BancoPopularLpt", employee: 0.01m, employer: 0.0025m),
        Rate("BancoPopularEmployerQuota", employee: 0m, employer: 0.0025m),
        Rate("FamilyAllowances", employee: 0m, employer: 0.05m),
        Rate("IMAS", employee: 0m, employer: 0.005m),
        Rate("INA", employee: 0m, employer: 0.015m),
        Rate("FCL", employee: 0m, employer: 0.015m),
        Rate("OPC", employee: 0m, employer: 0.02m),
        Rate("INS", employee: 0m, employer: 0.01m),
    ];

    public static readonly IReadOnlyList<ContributionMinimumBase> MinimumBases =
    [
        new() { ContributionCode = "SEM", MinimumBaseAmount = 346_789m },
        new() { ContributionCode = "IVM", MinimumBaseAmount = 324_590m },
    ];

    public static readonly IReadOnlyList<IncomeTaxBracket> IncomeTaxBrackets =
    [
        new() { LowerBoundAmount = 0m, UpperBoundAmount = 918_000m, TaxRate = 0m },
        new() { LowerBoundAmount = 918_000m, UpperBoundAmount = 1_347_000m, TaxRate = 0.10m },
        new() { LowerBoundAmount = 1_347_000m, UpperBoundAmount = 2_364_000m, TaxRate = 0.15m },
        new() { LowerBoundAmount = 2_364_000m, UpperBoundAmount = 4_727_000m, TaxRate = 0.20m },
        new() { LowerBoundAmount = 4_727_000m, UpperBoundAmount = null, TaxRate = 0.25m },
    ];

    public static PayrollCalculationInputs Inputs(
        DateTime periodStart,
        IReadOnlyList<CalculationEmployee> employees,
        IReadOnlyList<SalarySegment>? salarySegments = null,
        IReadOnlyList<WorkSchedule>? workSchedules = null,
        IReadOnlyList<ApprovedOvertime>? overtime = null,
        IReadOnlyList<ApprovedDisability>? disabilities = null,
        IReadOnlyList<CalculationConcept>? concepts = null)
    {
        return new PayrollCalculationInputs
        {
            PeriodStartDate = periodStart,
            PeriodEndDate = periodStart.AddMonths(1).AddDays(-1),
            Employees = employees,
            SalarySegments = salarySegments ?? [],
            WorkSchedules = workSchedules ?? [],
            Overtime = overtime ?? [],
            Disabilities = disabilities ?? [],
            Concepts = concepts ?? Concepts,
            ContributionRates = ContributionRates,
            MinimumBases = MinimumBases,
            IncomeTaxBrackets = IncomeTaxBrackets,
        };
    }

    public static CalculationEmployee Employee(
        int employeeId = 1,
        decimal baseSalary = 600_000m,
        DateTime? hireDate = null,
        DateTime? terminationDate = null)
    {
        return new CalculationEmployee
        {
            EmployeeId = employeeId,
            IdentificationNumber = $"1-0000-000{employeeId}",
            FirstName = "Ana",
            LastName = $"Prueba {employeeId}",
            DepartmentName = "Producción",
            JobTitle = "Operaria",
            HireDate = hireDate ?? new DateTime(2020, 1, 1),
            TerminationDate = terminationDate,
            BaseSalary = baseSalary,
        };
    }

    private static CalculationConcept Concept(
        int id,
        string code,
        string kind,
        bool taxable,
        bool subject,
        bool aguinaldo)
    {
        return new CalculationConcept
        {
            PayrollConceptId = id,
            PayrollConceptCode = code,
            Name = code,
            ConceptKind = kind,
            IsTaxableForIncomeTax = taxable,
            IsSubjectToSocialContributions = subject,
            CountsForAguinaldo = aguinaldo,
        };
    }

    private static ContributionRate Rate(
        string code,
        decimal employee,
        decimal employer,
        bool usesMinimumBase = false)
    {
        return new ContributionRate
        {
            ContributionCode = code,
            ContributionName = code,
            AppliesToEmployee = employee > 0m,
            AppliesToEmployer = employer > 0m,
            UsesMinimumBase = usesMinimumBase,
            EmployeeRate = employee,
            EmployerRate = employer,
        };
    }
}
