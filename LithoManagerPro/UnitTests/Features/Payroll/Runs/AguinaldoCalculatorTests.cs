using LithoManager.Application.Features.Payroll.Runs.Calculation;
using Xunit;

namespace LithoManager.UnitTests.Features.Payroll.Runs;

public sealed class AguinaldoCalculatorTests
{
    private static readonly AguinaldoRule CostaRicaRule = new()
    {
        AguinaldoRuleCode = "CostaRicaPrivateSector",
        Divisor = 12,
        IncludesOrdinarySalary = true,
        IncludesOvertime = true,
        IncludesSalaryInKind = true,
        ExcludesCommonIllnessSubsidy = true,
        IncludesMaternitySubsidy = true,
    };

    [Fact]
    public void Calculate_TwelveClosedMonths_DividesSalariesByTwelve()
    {
        List<AguinaldoSourceLine> lines = [];

        foreach ((int year, int month) in WindowMonths())
        {
            lines.Add(Line(year, month, PayrollConceptCodes.BaseSalary, 600_000m));
        }

        lines.Add(Line(2026, 3, PayrollConceptCodes.Overtime, 120_000m));

        PayrollCalculationResult result = AguinaldoCalculator.Calculate(
            Inputs(lines, closedPeriods: WindowMonths()));

        EmployeePayrollResult employee = Assert.Single(result.Employees);
        PayrollLineResult line = Assert.Single(employee.Lines);

        // (12 x 600,000 + 120,000) / 12
        Assert.Equal(610_000m, line.Amount);
        Assert.Equal(7_320_000m, line.BaseAmount);
        Assert.Equal(12m, line.Quantity);

        // No CCSS or income tax on the aguinaldo.
        Assert.Equal(610_000m, employee.NetAmount);
        Assert.Equal(0m, employee.EmployeeContributionsAmount);
        Assert.Equal(0m, employee.IncomeTaxAmount);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Calculate_MaternityDeductionIsAddedBack_CommonIllnessIsNot()
    {
        PayrollCalculationResult result = AguinaldoCalculator.Calculate(
            Inputs(
                [
                    Line(2026, 1, PayrollConceptCodes.BaseSalary, 600_000m),
                    Line(2026, 1, PayrollConceptCodes.BaseSalary, -60_000m, "CommonIllness"),
                    Line(2026, 2, PayrollConceptCodes.BaseSalary, 600_000m),
                    Line(2026, 2, PayrollConceptCodes.BaseSalary, -300_000m, "Maternity"),
                ],
                closedPeriods: [(2026, 1), (2026, 2)]));

        PayrollLineResult line = Assert.Single(
            Assert.Single(result.Employees).Lines);

        // 600,000 - 60,000 + 600,000 (maternity deduction ignored)
        Assert.Equal(1_140_000m, line.BaseAmount);
        Assert.Equal(95_000m, line.Amount);
    }

    [Fact]
    public void Calculate_MonthsWithoutClosedPayroll_AreReportedAsWarning()
    {
        PayrollCalculationResult result = AguinaldoCalculator.Calculate(
            Inputs(
                [Line(2026, 11, PayrollConceptCodes.BaseSalary, 600_000m)],
                closedPeriods: [(2026, 11)]));

        string warning = Assert.Single(result.Warnings);

        Assert.Contains("diciembre 2025", warning);
        Assert.Contains("octubre 2026", warning);
        Assert.DoesNotContain("noviembre 2026", warning);
        Assert.Equal(50_000m, Assert.Single(result.Employees).NetAmount);
    }

    [Fact]
    public void Calculate_WithoutRule_Throws()
    {
        AguinaldoCalculationInputs inputs = Inputs([], closedPeriods: []);

        Assert.Throws<PayrollCalculationException>(
            () => AguinaldoCalculator.Calculate(
                new AguinaldoCalculationInputs
                {
                    WindowStartDate = inputs.WindowStartDate,
                    WindowEndDate = inputs.WindowEndDate,
                    Concepts = inputs.Concepts,
                    Rule = null,
                }));
    }

    private static List<(int Year, int Month)> WindowMonths()
    {
        List<(int, int)> months = [(2025, 12)];

        for (int month = 1; month <= 11; month++)
        {
            months.Add((2026, month));
        }

        return months;
    }

    private static AguinaldoCalculationInputs Inputs(
        IReadOnlyList<AguinaldoSourceLine> lines,
        IReadOnlyList<(int Year, int Month)> closedPeriods)
    {
        return new AguinaldoCalculationInputs
        {
            WindowStartDate = new DateTime(2025, 12, 1),
            WindowEndDate = new DateTime(2026, 11, 30),
            Employees =
            [
                new AguinaldoEmployee
                {
                    EmployeeId = 1,
                    IdentificationNumber = "1-0000-0001",
                    EmployeeName = "Ana Prueba",
                    MonthlyBaseSalary = 600_000m,
                },
            ],
            SourceLines = lines,
            ClosedPeriods = closedPeriods
                .Select(p => new ClosedPayrollPeriod
                {
                    PeriodYear = p.Year,
                    PeriodMonth = p.Month,
                })
                .ToList(),
            Rule = CostaRicaRule,
            DisabilityTypes =
            [
                new() { DisabilityTypeCode = "CommonIllness", CountsAsSalaryForAguinaldo = false },
                new() { DisabilityTypeCode = "Maternity", CountsAsSalaryForAguinaldo = true },
            ],
            Concepts = PayrollTestData.Concepts,
        };
    }

    private static AguinaldoSourceLine Line(
        int year,
        int month,
        string conceptCode,
        decimal amount,
        string? referenceCode = null)
    {
        return new AguinaldoSourceLine
        {
            EmployeeId = 1,
            PeriodYear = year,
            PeriodMonth = month,
            PayrollConceptCode = conceptCode,
            ReferenceCode = referenceCode,
            Amount = amount,
        };
    }
}
