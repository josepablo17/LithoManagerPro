using LithoManager.Application.Features.Payroll.Runs.Calculation;
using Xunit;
using static LithoManager.UnitTests.Features.Payroll.Runs.PayrollTestData;

namespace LithoManager.UnitTests.Features.Payroll.Runs;

public sealed class PayrollCalculatorTests
{
    private static readonly DateTime October2026 = new(2026, 10, 1);
    private static readonly DateTime November2026 = new(2026, 11, 1);

    [Fact]
    public void Calculate_WholeMonthWith31Days_PaysExactlyTheMonthlySalary()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(October2026, [Employee(baseSalary: 500_000m)]));

        EmployeePayrollResult employee = Assert.Single(result.Employees);

        Assert.Equal(500_000m, BaseSalary(employee));
        Assert.Equal(30m, employee.PaidDays);
    }

    [Fact]
    public void Calculate_HiredMidMonth_PaysCalendarDaysOverThirty()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(
                November2026,
                [Employee(baseSalary: 600_000m, hireDate: new DateTime(2026, 11, 16))]));

        EmployeePayrollResult employee = Assert.Single(result.Employees);

        // 15 days (16-30 Nov) x 600,000 / 30
        Assert.Equal(300_000m, BaseSalary(employee));
        Assert.Equal(15m, employee.PaidDays);
    }

    [Fact]
    public void Calculate_TerminatedMidMonth_PaysUntilTerminationDate()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(
                November2026,
                [Employee(baseSalary: 600_000m, terminationDate: new DateTime(2026, 11, 10))]));

        EmployeePayrollResult employee = Assert.Single(result.Employees);

        Assert.Equal(200_000m, BaseSalary(employee));
    }

    [Fact]
    public void Calculate_EmployeeOutsideThePeriod_IsSkipped()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(
                November2026,
                [Employee(hireDate: new DateTime(2026, 12, 1))]));

        Assert.Empty(result.Employees);
    }

    [Fact]
    public void Calculate_SalaryChangeMidMonth_ProratesEachSalaryByDay()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(
                November2026,
                [Employee(baseSalary: 900_000m)],
                salarySegments:
                [
                    Segment(600_000m, new DateTime(2025, 1, 1), new DateTime(2026, 11, 15)),
                    Segment(900_000m, new DateTime(2026, 11, 16), null),
                ]));

        EmployeePayrollResult employee = Assert.Single(result.Employees);
        PayrollLineResult[] salaryLines = Lines(employee, PayrollConceptCodes.BaseSalary);

        Assert.Equal(2, salaryLines.Length);
        Assert.Equal(300_000m, salaryLines[0].Amount);
        Assert.Equal(450_000m, salaryLines[1].Amount);
        Assert.Equal(750_000m, employee.GrossAmount);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Calculate_DaysWithoutSalaryHistory_UseCurrentSalaryAndWarn()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(
                November2026,
                [Employee(baseSalary: 600_000m)],
                salarySegments: [Segment(600_000m, new DateTime(2026, 11, 4), null)]));

        Assert.Equal(600_000m, BaseSalary(Assert.Single(result.Employees)));
        Assert.Contains(result.Warnings, w => w.Contains("historial salarial"));
    }

    [Fact]
    public void Calculate_RegularSalary_AppliesEmployeeAndEmployerContributions()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(November2026, [Employee(baseSalary: 500_000m)]));

        EmployeePayrollResult employee = Assert.Single(result.Employees);

        // Employee: SEM 5.5% + IVM 4.33% + Banco Popular 1% = 10.83%
        Assert.Equal(27_500m, Contribution(employee, PayrollConceptCodes.EmployeeSocialContribution, "SEM"));
        Assert.Equal(21_650m, Contribution(employee, PayrollConceptCodes.EmployeeSocialContribution, "IVM"));
        Assert.Equal(5_000m, Contribution(employee, PayrollConceptCodes.EmployeeSocialContribution, "BancoPopularLpt"));
        Assert.Equal(54_150m, employee.EmployeeContributionsAmount);

        // Employer: 26.83% of 500,000
        Assert.Equal(134_150m, employee.EmployerContributionsAmount);

        // CCSS lines come first, in the order SEM, IVM.
        PayrollLineResult[] employeeLines = employee.Lines
            .Where(l => l.PayrollConceptCode == PayrollConceptCodes.EmployeeSocialContribution)
            .ToArray();

        Assert.Equal(["SEM", "IVM", "BancoPopularLpt"], employeeLines.Select(l => l.ReferenceCode));
    }

    [Fact]
    public void Calculate_SalaryBelowMinimumBase_EmployerCoversTheDifference()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(November2026, [Employee(baseSalary: 288_000m)]));

        EmployeePayrollResult employee = Assert.Single(result.Employees);

        // The employee always pays on the actual salary.
        Assert.Equal(15_840m, Contribution(employee, PayrollConceptCodes.EmployeeSocialContribution, "SEM"));
        Assert.Equal(12_470.40m, Contribution(employee, PayrollConceptCodes.EmployeeSocialContribution, "IVM"));

        // SEM: 346,789 x 9.25% on the minimum base, plus the employee's
        // 5.5% on the 58,789 difference, as two lines that each add up.
        PayrollLineResult[] employerSem = employee.Lines
            .Where(l => l.PayrollConceptCode == PayrollConceptCodes.EmployerSocialContribution
                && l.ReferenceCode == "SEM")
            .ToArray();

        Assert.Equal(2, employerSem.Length);
        Assert.Equal(346_789m, employerSem[0].BaseAmount);
        Assert.Equal(32_077.98m, employerSem[0].Amount);
        Assert.Equal(58_789m, employerSem[1].BaseAmount);
        Assert.Equal(0.055m, employerSem[1].Rate);
        Assert.Equal(3_233.40m, employerSem[1].Amount);

        // IVM: 324,590 x 5.58% + (324,590 - 288,000) x 4.33%
        Assert.Equal(19_696.47m, EmployerTotal(employee, "IVM"));
        Assert.Equal(89_567.85m, employee.EmployerContributionsAmount);

        // Contributions without a minimum base use the actual salary.
        Assert.Equal(14_400m, Contribution(employee, PayrollConceptCodes.EmployerSocialContribution, "FamilyAllowances"));
    }

    [Theory]
    [InlineData(900_000, 0)]
    [InlineData(918_000, 0)]
    [InlineData(1_000_000, 8_200)]
    [InlineData(1_500_000, 65_850)]
    [InlineData(5_000_000, 736_300)]
    public void CalculateIncomeTax_AppliesProgressiveBrackets(
        decimal taxableAmount,
        decimal expectedTax)
    {
        // 5,000,000: 42,900 (10%) + 152,550 (15%) + 472,600 (20%)
        // + 68,250 (25% above 4,727,000) = 736,300
        decimal tax = PayrollCalculator.CalculateIncomeTax(
            taxableAmount,
            IncomeTaxBrackets);

        Assert.Equal(expectedTax, Math.Round(tax, 2));
    }

    [Fact]
    public void Calculate_SalaryAboveTaxExemption_WithholdsIncomeTax()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(November2026, [Employee(baseSalary: 1_500_000m)]));

        EmployeePayrollResult employee = Assert.Single(result.Employees);

        Assert.Equal(65_850m, employee.IncomeTaxAmount);
        Assert.Equal(
            employee.GrossAmount - employee.EmployeeContributionsAmount - employee.IncomeTaxAmount,
            employee.NetAmount);
    }

    [Fact]
    public void Calculate_ApprovedOvertime_PaysHourlyRateTimesMultiplier()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(
                November2026,
                [Employee(baseSalary: 600_000m)],
                workSchedules: [DayShift()],
                overtime:
                [
                    Overtime(new DateTime(2026, 11, 5), 3m, "Standard", 1.5m),
                    Overtime(new DateTime(2026, 11, 6), 1m, "Standard", 1.5m),
                    Overtime(new DateTime(2026, 11, 7), 2m, "Holiday", 2m),
                ]));

        EmployeePayrollResult employee = Assert.Single(result.Employees);
        PayrollLineResult[] overtime = Lines(employee, PayrollConceptCodes.Overtime);

        // Hourly rate: 600,000 / 30 / 8 = 2,500
        PayrollLineResult holiday = Assert.Single(overtime, l => l.ReferenceCode == "Holiday");
        PayrollLineResult standard = Assert.Single(overtime, l => l.ReferenceCode == "Standard");

        Assert.Equal(4m, standard.Quantity);
        Assert.Equal(15_000m, standard.Amount);
        Assert.Equal(10_000m, holiday.Amount);
        Assert.Equal(625_000m, employee.GrossAmount);

        // Overtime is subject to CCSS: 10.83% of 625,000
        Assert.Equal(67_687.50m, employee.EmployeeContributionsAmount);
        Assert.DoesNotContain(result.Warnings, w => w.Contains("no tiene horario"));
    }

    [Fact]
    public void Calculate_OvertimeWithoutSchedule_UsesEightHoursAndWarns()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(
                November2026,
                [Employee(baseSalary: 600_000m)],
                overtime: [Overtime(new DateTime(2026, 11, 5), 2m, "Standard", 1.5m)]));

        EmployeePayrollResult employee = Assert.Single(result.Employees);

        Assert.Equal(7_500m, Assert.Single(Lines(employee, PayrollConceptCodes.Overtime)).Amount);
        Assert.Contains(result.Warnings, w => w.Contains("no tiene horario"));
    }

    [Fact]
    public void Calculate_Disability_DeductsDaysAndAddsEmployerSubsidy()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(
                November2026,
                [Employee(baseSalary: 600_000m)],
                disabilities:
                [
                    Disability(
                        "CommonIllness",
                        new DateTime(2026, 11, 10),
                        new DateTime(2026, 11, 12),
                        employerPaidAmount: 30_000m),
                ]));

        EmployeePayrollResult employee = Assert.Single(result.Employees);

        PayrollLineResult deduction = Assert.Single(
            employee.Lines,
            l => l.ReferenceCode == "CommonIllness"
                && l.PayrollConceptCode == PayrollConceptCodes.BaseSalary);

        // 3 days x 600,000 / 30
        Assert.Equal(-60_000m, deduction.Amount);
        Assert.Equal(540_000m, employee.GrossAmount);
        Assert.Equal(27m, employee.PaidDays);

        // The subsidy is paid but is not salary: no CCSS on it.
        Assert.Equal(30_000m, Assert.Single(Lines(employee, PayrollConceptCodes.DisabilitySubsidy)).Amount);
        Assert.Equal(58_482m, employee.EmployeeContributionsAmount);
        Assert.Equal(540_000m + 30_000m - 58_482m, employee.NetAmount);
    }

    [Fact]
    public void Calculate_OverlappingDisabilities_DeductEachDayOnlyOnce()
    {
        PayrollCalculationResult result = PayrollCalculator.Calculate(
            Inputs(
                November2026,
                [Employee(baseSalary: 600_000m)],
                disabilities:
                [
                    Disability("CommonIllness", new DateTime(2026, 11, 10), new DateTime(2026, 11, 14)),
                    Disability("OccupationalRisk", new DateTime(2026, 11, 13), new DateTime(2026, 11, 16)),
                ]));

        EmployeePayrollResult employee = Assert.Single(result.Employees);

        // 10-16 Nov = 7 distinct days
        Assert.Equal(460_000m, employee.GrossAmount);
        Assert.Equal(23m, employee.PaidDays);
    }

    [Fact]
    public void Calculate_MissingRequiredConcept_Throws()
    {
        PayrollCalculationInputs inputs = Inputs(
            November2026,
            [Employee()],
            concepts: Concepts
                .Where(c => c.PayrollConceptCode != PayrollConceptCodes.IncomeTax)
                .ToList());

        // Income tax is only required when there is tax to withhold.
        PayrollCalculator.Calculate(inputs);

        PayrollCalculationInputs withoutSalary = Inputs(
            November2026,
            [Employee()],
            concepts: Concepts
                .Where(c => c.PayrollConceptCode != PayrollConceptCodes.BaseSalary)
                .ToList());

        Assert.Throws<PayrollCalculationException>(
            () => PayrollCalculator.Calculate(withoutSalary));
    }

    private static decimal BaseSalary(EmployeePayrollResult employee)
    {
        return employee.Lines
            .Where(l => l.PayrollConceptCode == PayrollConceptCodes.BaseSalary
                && l.ReferenceCode is null)
            .Sum(l => l.Amount);
    }

    private static PayrollLineResult[] Lines(
        EmployeePayrollResult employee,
        string conceptCode)
    {
        return employee.Lines
            .Where(l => l.PayrollConceptCode == conceptCode)
            .ToArray();
    }

    private static decimal Contribution(
        EmployeePayrollResult employee,
        string conceptCode,
        string contributionCode)
    {
        return Assert.Single(
                employee.Lines,
                l => l.PayrollConceptCode == conceptCode
                    && l.ReferenceCode == contributionCode)
            .Amount;
    }

    private static decimal EmployerTotal(
        EmployeePayrollResult employee,
        string contributionCode)
    {
        return employee.Lines
            .Where(l => l.PayrollConceptCode == PayrollConceptCodes.EmployerSocialContribution
                && l.ReferenceCode == contributionCode)
            .Sum(l => l.Amount);
    }

    private static SalarySegment Segment(
        decimal salary,
        DateTime from,
        DateTime? to)
    {
        return new SalarySegment
        {
            EmployeeId = 1,
            BaseSalary = salary,
            EffectiveFromDate = from,
            EffectiveToDate = to,
        };
    }

    private static WorkSchedule DayShift()
    {
        return new WorkSchedule
        {
            EmployeeId = 1,
            WorkShiftTypeCode = "Day",
            MaxOrdinaryHoursPerDay = 8m,
            EffectiveFromDate = new DateTime(2020, 1, 1),
        };
    }

    private static ApprovedOvertime Overtime(
        DateTime date,
        decimal hours,
        string ruleCode,
        decimal multiplier)
    {
        return new ApprovedOvertime
        {
            EmployeeId = 1,
            OvertimeDate = date,
            Hours = hours,
            OvertimeRuleCode = ruleCode,
            OvertimeRuleName = ruleCode,
            HourMultiplier = multiplier,
        };
    }

    private static ApprovedDisability Disability(
        string typeCode,
        DateTime start,
        DateTime end,
        decimal? employerPaidAmount = null)
    {
        return new ApprovedDisability
        {
            EmployeeId = 1,
            DisabilityTypeCode = typeCode,
            DisabilityTypeName = typeCode,
            ReducesWorkedDays = true,
            StartDate = start,
            EndDate = end,
            EmployerPaidAmount = employerPaidAmount,
        };
    }
}
