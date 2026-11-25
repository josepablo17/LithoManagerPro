using System.Globalization;

namespace LithoManager.Application.Features.Payroll.Runs.Calculation;

/// <summary>
/// Calculates the Costa Rica aguinaldo (thirteenth-month salary) from the
/// closed regular payrolls inside the rule's window (1 December to
/// 30 November): the sum of the included salaries divided by the rule's
/// divisor (12). The aguinaldo is not subject to CCSS or income tax.
///
/// Which amounts count is driven by the aguinaldo rule:
/// <list type="bullet">
/// <item>Ordinary salary, overtime and salary in kind, each behind its
/// own flag.</item>
/// <item>Disability deductions stay deducted (the common illness subsidy
/// does not count as salary), except for disability types that count as
/// salary for the aguinaldo (maternity), whose deduction is added back
/// when the rule includes the maternity subsidy.</item>
/// </list>
/// </summary>
public static class AguinaldoCalculator
{
    private static readonly CultureInfo SpanishCulture =
        CultureInfo.GetCultureInfo("es-CR");

    public static PayrollCalculationResult Calculate(
        AguinaldoCalculationInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        AguinaldoRule rule = inputs.Rule
            ?? throw new PayrollCalculationException(
                "No active aguinaldo rule is effective for the period.");

        if (rule.Divisor <= 0)
        {
            throw new PayrollCalculationException(
                "The aguinaldo rule divisor must be greater than zero.");
        }

        ConceptCatalog concepts = new(inputs.Concepts);
        CalculationConcept aguinaldoConcept =
            concepts.Require(PayrollConceptCodes.Aguinaldo);

        HashSet<string> disabilitiesCountingAsSalary = inputs.DisabilityTypes
            .Where(t => t.CountsAsSalaryForAguinaldo)
            .Select(t => t.DisabilityTypeCode)
            .ToHashSet(StringComparer.Ordinal);

        List<string> warnings = BuildWarnings(inputs);
        List<EmployeePayrollResult> employees = [];

        foreach (AguinaldoEmployee employee in inputs.Employees
                     .OrderBy(e => e.EmployeeId))
        {
            List<AguinaldoSourceLine> sourceLines = inputs.SourceLines
                .Where(l => l.EmployeeId == employee.EmployeeId)
                .ToList();

            decimal baseAmount = PayrollCalculator.Round(
                sourceLines
                    .Where(l => Counts(l, rule, disabilitiesCountingAsSalary))
                    .Sum(l => l.Amount));

            if (baseAmount <= 0m)
            {
                continue;
            }

            int months = sourceLines
                .Select(l => (l.PeriodYear, l.PeriodMonth))
                .Distinct()
                .Count();

            decimal amount = PayrollCalculator.Round(baseAmount / rule.Divisor);

            PayrollLineResult line = PayrollCalculator.CreateLine(
                aguinaldoConcept,
                referenceCode: rule.AguinaldoRuleCode,
                description:
                    $"Aguinaldo: salarios de {months} {(months == 1 ? "mes" : "meses")} / {rule.Divisor}",
                quantity: months,
                rate: null,
                baseAmount: baseAmount,
                amount: amount,
                sortOrder: 100);

            employees.Add(new EmployeePayrollResult
            {
                EmployeeId = employee.EmployeeId,
                IdentificationNumber = employee.IdentificationNumber,
                EmployeeName = employee.EmployeeName,
                DepartmentName = employee.DepartmentName,
                JobTitle = employee.JobTitle,
                MonthlyBaseSalary = employee.MonthlyBaseSalary,
                PaidDays = 0m,
                GrossAmount = amount,
                EmployeeContributionsAmount = 0m,
                IncomeTaxAmount = 0m,
                NetAmount = amount,
                EmployerContributionsAmount = 0m,
                Lines = [line],
            });
        }

        return new PayrollCalculationResult
        {
            Employees = employees,
            Warnings = warnings,
        };
    }

    private static bool Counts(
        AguinaldoSourceLine line,
        AguinaldoRule rule,
        HashSet<string> disabilitiesCountingAsSalary)
    {
        return line.PayrollConceptCode switch
        {
            // Base salary lines have no reference code; disability
            // deductions carry the disability type code.
            PayrollConceptCodes.BaseSalary when line.ReferenceCode is null =>
                rule.IncludesOrdinarySalary,

            PayrollConceptCodes.BaseSalary =>
                rule.IncludesOrdinarySalary
                && !(rule.IncludesMaternitySubsidy
                     && disabilitiesCountingAsSalary.Contains(line.ReferenceCode)),

            PayrollConceptCodes.Overtime => rule.IncludesOvertime,
            PayrollConceptCodes.SalaryInKind => rule.IncludesSalaryInKind,
            _ => false,
        };
    }

    private static List<string> BuildWarnings(
        AguinaldoCalculationInputs inputs)
    {
        List<string> warnings = [];

        if (inputs.ClosedPeriods.Count == 0)
        {
            warnings.Add(
                "No hay planillas regulares cerradas en el período del aguinaldo; no hay salarios para calcularlo.");

            return warnings;
        }

        HashSet<(int Year, int Month)> closed = inputs.ClosedPeriods
            .Select(p => (p.PeriodYear, p.PeriodMonth))
            .ToHashSet();

        List<string> missing = [];

        for (DateTime month = new(inputs.WindowStartDate.Year, inputs.WindowStartDate.Month, 1);
             month <= inputs.WindowEndDate;
             month = month.AddMonths(1))
        {
            if (!closed.Contains((month.Year, month.Month)))
            {
                missing.Add(month.ToString("MMMM yyyy", SpanishCulture));
            }
        }

        if (missing.Count > 0)
        {
            warnings.Add(
                $"Meses sin planilla regular cerrada, que no suman al aguinaldo: {string.Join(", ", missing)}.");
        }

        return warnings;
    }
}
