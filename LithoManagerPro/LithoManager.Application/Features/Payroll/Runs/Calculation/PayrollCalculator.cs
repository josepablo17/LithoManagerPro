namespace LithoManager.Application.Features.Payroll.Runs.Calculation;

/// <summary>
/// Calculates a monthly (regular) payroll for Costa Rica. It is a pure
/// function of its inputs: no database, clock or configuration access,
/// which is what makes every rule unit-testable.
///
/// Rules applied:
/// <list type="bullet">
/// <item>Monthly salaries are paid on a 30-day commercial month. An
/// employee who works the whole calendar month earns exactly the monthly
/// salary; a partial month (hire or termination inside the period) pays
/// salary / 30 per calendar day worked.</item>
/// <item>Salary changes inside the month are prorated day by day using
/// the salary history; days without history use the current salary.</item>
/// <item>Approved disabilities flagged as reducing worked days deduct the
/// salary of those days. The amount the employer pays for the disability
/// is added as a non-taxable subsidy.</item>
/// <item>Approved overtime is paid at salary / 30 / ordinary hours per day
/// of the employee's shift, times the rule multiplier.</item>
/// <item>CCSS contributions apply to concepts subject to social
/// contributions. When the base is below the minimum contribution base
/// (prorated by the days employed), the employee pays on the actual
/// salary and the employer covers the difference up to the minimum.</item>
/// <item>Income tax applies the progressive monthly brackets to the
/// taxable concepts.</item>
/// </list>
/// </summary>
public static class PayrollCalculator
{
    public const int CommercialMonthDays = 30;
    public const decimal DefaultOrdinaryHoursPerDay = 8m;

    private const int BaseSalarySortOrder = 100;
    private const int DisabilitySortOrder = 200;
    private const int OvertimeSortOrder = 300;
    private const int SubsidySortOrder = 400;
    private const int EmployeeContributionSortOrder = 500;
    private const int IncomeTaxSortOrder = 600;
    private const int EmployerContributionSortOrder = 700;

    public static PayrollCalculationResult Calculate(
        PayrollCalculationInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        ConceptCatalog concepts = new(inputs.Concepts);
        List<string> warnings = [];
        List<EmployeePayrollResult> employees = [];

        if (inputs.IncomeTaxBrackets.Count == 0)
        {
            warnings.Add(
                "No hay tramos de impuesto sobre la renta vigentes para el período; no se retuvo renta.");
        }

        if (inputs.ContributionRates.Count == 0)
        {
            warnings.Add(
                "No hay tasas de cargas sociales vigentes para el período; no se calcularon cargas CCSS.");
        }

        foreach (CalculationEmployee employee in inputs.Employees
                     .OrderBy(e => e.EmployeeId))
        {
            EmployeePayrollResult? result =
                CalculateEmployee(
                    employee,
                    inputs,
                    concepts,
                    warnings);

            if (result is not null)
            {
                employees.Add(result);
            }
        }

        return new PayrollCalculationResult
        {
            Employees = employees,
            Warnings = warnings,
        };
    }

    private static EmployeePayrollResult? CalculateEmployee(
        CalculationEmployee employee,
        PayrollCalculationInputs inputs,
        ConceptCatalog concepts,
        List<string> warnings)
    {
        DateTime periodStart = inputs.PeriodStartDate.Date;
        DateTime periodEnd = inputs.PeriodEndDate.Date;
        int daysInPeriod = (periodEnd - periodStart).Days + 1;

        DateTime employedFrom = Max(employee.HireDate.Date, periodStart);
        DateTime employedTo = Min(
            employee.TerminationDate?.Date ?? periodEnd,
            periodEnd);

        if (employedFrom > employedTo)
        {
            return null;
        }

        string employeeName =
            $"{employee.FirstName} {employee.LastName}".Trim();

        bool worksWholeMonth =
            employedFrom == periodStart && employedTo == periodEnd;

        // Whole month: each calendar day is worth 1/daysInPeriod of the
        // salary, so the month always adds up to the monthly salary.
        // Partial month: each day is worth salary / 30.
        int dayDivisor = worksWholeMonth
            ? daysInPeriod
            : CommercialMonthDays;

        decimal dayWeight = (decimal)CommercialMonthDays / dayDivisor;

        SalaryTimeline salaries = new(
            employee,
            inputs.SalarySegments
                .Where(s => s.EmployeeId == employee.EmployeeId));

        List<PayrollLineResult> lines = [];

        // Base salary, one line per salary in effect during the month.
        int employedDays = 0;
        List<(decimal Salary, DateTime From, int Days)> salaryGroups = [];

        for (DateTime day = employedFrom; day <= employedTo; day = day.AddDays(1))
        {
            decimal salary = salaries.SalaryOn(day);
            employedDays++;

            if (salaryGroups.Count > 0
                && salaryGroups[^1].Salary == salary)
            {
                (decimal s, DateTime f, int d) = salaryGroups[^1];
                salaryGroups[^1] = (s, f, d + 1);
            }
            else
            {
                salaryGroups.Add((salary, day, 1));
            }
        }

        for (int index = 0; index < salaryGroups.Count; index++)
        {
            (decimal salary, DateTime from, int days) = salaryGroups[index];

            lines.Add(CreateLine(
                concepts.Require(PayrollConceptCodes.BaseSalary),
                referenceCode: null,
                description: salaryGroups.Count == 1
                    ? "Salario base"
                    : $"Salario base desde el {from:dd/MM}",
                quantity: Round(days * dayWeight),
                rate: RoundRate(salary / dayDivisor),
                baseAmount: salary,
                amount: Round(salary * days / dayDivisor),
                sortOrder: BaseSalarySortOrder + index));
        }

        if (salaries.UsedFallback)
        {
            warnings.Add(
                $"{employeeName}: no tiene historial salarial para todos los días del período; se usó el salario actual.");
        }

        // Disabilities. A day can only be deducted once, even if two
        // disability records overlap.
        HashSet<DateTime> deductedDays = [];
        int disabilityIndex = 0;

        foreach (ApprovedDisability disability in inputs.Disabilities
                     .Where(d => d.EmployeeId == employee.EmployeeId)
                     .OrderBy(d => d.StartDate))
        {
            DateTime overlapFrom = Max(disability.StartDate.Date, employedFrom);
            DateTime overlapTo = Min(disability.EndDate.Date, employedTo);

            if (overlapFrom > overlapTo)
            {
                continue;
            }

            if (disability.ReducesWorkedDays)
            {
                int days = 0;
                decimal deduction = 0m;

                for (DateTime day = overlapFrom; day <= overlapTo; day = day.AddDays(1))
                {
                    if (deductedDays.Add(day))
                    {
                        days++;
                        deduction += salaries.SalaryOn(day) / dayDivisor;
                    }
                }

                if (days > 0)
                {
                    lines.Add(CreateLine(
                        concepts.Require(PayrollConceptCodes.BaseSalary),
                        referenceCode: disability.DisabilityTypeCode,
                        description:
                            $"Rebajo por incapacidad ({DisabilityLabel(disability)}) del {overlapFrom:dd/MM} al {overlapTo:dd/MM}",
                        quantity: Round(days * dayWeight),
                        rate: null,
                        baseAmount: null,
                        amount: -Round(deduction),
                        sortOrder: DisabilitySortOrder + disabilityIndex));
                }
            }

            if (disability.EmployerPaidAmount is > 0m
                && concepts.Find(PayrollConceptCodes.DisabilitySubsidy)
                    is CalculationConcept subsidyConcept)
            {
                int totalDays =
                    (disability.EndDate.Date - disability.StartDate.Date).Days + 1;
                int overlapDays = (overlapTo - overlapFrom).Days + 1;

                lines.Add(CreateLine(
                    subsidyConcept,
                    referenceCode: disability.DisabilityTypeCode,
                    description:
                        $"Subsidio pagado por el patrono ({DisabilityLabel(disability)})",
                    quantity: overlapDays,
                    rate: null,
                    baseAmount: disability.EmployerPaidAmount,
                    amount: Round(
                        disability.EmployerPaidAmount.Value
                        * overlapDays / totalDays),
                    sortOrder: SubsidySortOrder + disabilityIndex));
            }

            disabilityIndex++;
        }

        // Overtime, grouped by rule.
        List<ApprovedOvertime> overtime = inputs.Overtime
            .Where(o => o.EmployeeId == employee.EmployeeId)
            .ToList();

        bool usedDefaultHours = false;
        int overtimeIndex = 0;

        foreach (IGrouping<string, ApprovedOvertime> rule in overtime
                     .GroupBy(o => o.OvertimeRuleCode)
                     .OrderBy(g => g.Key))
        {
            decimal hours = 0m;
            decimal amount = 0m;

            foreach (ApprovedOvertime record in rule)
            {
                decimal ordinaryHours = OrdinaryHoursPerDay(
                    inputs.WorkSchedules,
                    employee.EmployeeId,
                    record.OvertimeDate.Date,
                    ref usedDefaultHours);

                decimal hourlyRate =
                    salaries.SalaryOn(record.OvertimeDate.Date)
                    / CommercialMonthDays
                    / ordinaryHours;

                hours += record.Hours;
                amount += record.Hours * hourlyRate * record.HourMultiplier;
            }

            ApprovedOvertime first = rule.First();

            lines.Add(CreateLine(
                concepts.Require(PayrollConceptCodes.Overtime),
                referenceCode: rule.Key,
                description:
                    $"Horas extra {OvertimeLabel(first)} (x{first.HourMultiplier:0.##})",
                quantity: hours,
                rate: first.HourMultiplier,
                baseAmount: null,
                amount: Round(amount),
                sortOrder: OvertimeSortOrder + overtimeIndex++));
        }

        if (usedDefaultHours)
        {
            warnings.Add(
                $"{employeeName}: no tiene horario asignado; las horas extra se pagaron con una jornada de {DefaultOrdinaryHoursPerDay:0} horas.");
        }

        decimal grossAmount = lines
            .Where(l => l.ConceptKind == PayrollConceptKinds.Earning)
            .Sum(l => l.Amount);

        decimal socialContributionBase = lines
            .Where(l => concepts.IsSubjectToSocialContributions(l.PayrollConceptId))
            .Sum(l => l.Amount);

        decimal taxableAmount = lines
            .Where(l => concepts.IsTaxable(l.PayrollConceptId))
            .Sum(l => l.Amount);

        // Social contributions (CCSS and LPT).
        decimal employeeContributions = 0m;
        decimal employerContributions = 0m;

        if (socialContributionBase > 0m)
        {
            decimal employmentFraction = Math.Min(
                1m,
                employedDays * dayWeight / CommercialMonthDays);

            int contributionIndex = 0;

            foreach (ContributionRate rate in inputs.ContributionRates
                         .OrderBy(r => ContributionDisplayOrder(r.ContributionCode))
                         .ThenBy(r => r.ContributionCode, StringComparer.Ordinal))
            {
                decimal minimumBase = rate.UsesMinimumBase
                    ? Round(MinimumBaseFor(inputs.MinimumBases, rate.ContributionCode)
                        * employmentFraction)
                    : 0m;

                bool isBelowMinimum =
                    minimumBase > 0m && socialContributionBase < minimumBase;

                string label = ContributionLabel(rate);

                if (rate.AppliesToEmployee && rate.EmployeeRate > 0m)
                {
                    decimal amount = Round(socialContributionBase * rate.EmployeeRate);
                    employeeContributions += amount;

                    lines.Add(CreateLine(
                        concepts.Require(PayrollConceptCodes.EmployeeSocialContribution),
                        referenceCode: rate.ContributionCode,
                        description: label,
                        quantity: null,
                        rate: rate.EmployeeRate,
                        baseAmount: socialContributionBase,
                        amount: amount,
                        sortOrder: EmployeeContributionSortOrder + contributionIndex));
                }

                if (rate.AppliesToEmployer && rate.EmployerRate > 0m)
                {
                    decimal employerBase = isBelowMinimum
                        ? minimumBase
                        : socialContributionBase;

                    decimal amount = Round(employerBase * rate.EmployerRate);
                    employerContributions += amount;

                    lines.Add(CreateLine(
                        concepts.Require(PayrollConceptCodes.EmployerSocialContribution),
                        referenceCode: rate.ContributionCode,
                        description: isBelowMinimum
                            ? $"{label} sobre la base mínima contributiva"
                            : label,
                        quantity: null,
                        rate: rate.EmployerRate,
                        baseAmount: employerBase,
                        amount: amount,
                        sortOrder: EmployerContributionSortOrder + contributionIndex * 2));

                    // Below the minimum base the employer also pays the
                    // employee's share of the difference. It is a separate
                    // line so that every line is exactly rate x base.
                    if (isBelowMinimum && rate.AppliesToEmployee && rate.EmployeeRate > 0m)
                    {
                        decimal difference = minimumBase - socialContributionBase;
                        decimal coveredShare = Round(difference * rate.EmployeeRate);
                        employerContributions += coveredShare;

                        lines.Add(CreateLine(
                            concepts.Require(PayrollConceptCodes.EmployerSocialContribution),
                            referenceCode: rate.ContributionCode,
                            description: $"{label}: cuota obrera de la diferencia hasta la base mínima",
                            quantity: null,
                            rate: rate.EmployeeRate,
                            baseAmount: difference,
                            amount: coveredShare,
                            sortOrder: EmployerContributionSortOrder + contributionIndex * 2 + 1));
                    }
                }

                contributionIndex++;
            }
        }

        // Income tax.
        decimal incomeTax = Round(
            CalculateIncomeTax(taxableAmount, inputs.IncomeTaxBrackets));

        if (incomeTax > 0m)
        {
            lines.Add(CreateLine(
                concepts.Require(PayrollConceptCodes.IncomeTax),
                referenceCode: null,
                description: "Impuesto sobre la renta",
                quantity: null,
                rate: null,
                baseAmount: taxableAmount,
                amount: incomeTax,
                sortOrder: IncomeTaxSortOrder));
        }

        decimal subsidies = lines
            .Where(l => l.ConceptKind == PayrollConceptKinds.Informational)
            .Sum(l => l.Amount);

        decimal paidDays = Round(
            (employedDays - deductedDays.Count) * dayWeight);

        return new EmployeePayrollResult
        {
            EmployeeId = employee.EmployeeId,
            IdentificationNumber = employee.IdentificationNumber,
            EmployeeName = employeeName,
            DepartmentName = employee.DepartmentName,
            JobTitle = employee.JobTitle,
            MonthlyBaseSalary = salaries.SalaryOn(employedTo),
            PaidDays = paidDays,
            GrossAmount = grossAmount,
            EmployeeContributionsAmount = employeeContributions,
            IncomeTaxAmount = incomeTax,
            NetAmount = grossAmount + subsidies - employeeContributions - incomeTax,
            EmployerContributionsAmount = employerContributions,
            Lines = lines.OrderBy(l => l.SortOrder).ToList(),
        };
    }

    /// <summary>Progressive tax: each bracket taxes only its own slice.</summary>
    public static decimal CalculateIncomeTax(
        decimal taxableAmount,
        IReadOnlyList<IncomeTaxBracket> brackets)
    {
        ArgumentNullException.ThrowIfNull(brackets);

        decimal tax = 0m;

        foreach (IncomeTaxBracket bracket in brackets
                     .OrderBy(b => b.LowerBoundAmount))
        {
            if (taxableAmount <= bracket.LowerBoundAmount)
            {
                break;
            }

            decimal upper = bracket.UpperBoundAmount is decimal bound
                ? Math.Min(taxableAmount, bound)
                : taxableAmount;

            tax += (upper - bracket.LowerBoundAmount) * bracket.TaxRate;
        }

        return tax;
    }

    internal static decimal Round(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal RoundRate(decimal value)
    {
        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }

    internal static PayrollLineResult CreateLine(
        CalculationConcept concept,
        string? referenceCode,
        string description,
        decimal? quantity,
        decimal? rate,
        decimal? baseAmount,
        decimal amount,
        int sortOrder)
    {
        return new PayrollLineResult
        {
            PayrollConceptId = concept.PayrollConceptId,
            PayrollConceptCode = concept.PayrollConceptCode,
            ConceptKind = concept.ConceptKind,
            ReferenceCode = referenceCode,
            Description = description,
            Quantity = quantity,
            Rate = rate,
            BaseAmount = baseAmount,
            Amount = amount,
            SortOrder = sortOrder,
        };
    }

    private static decimal OrdinaryHoursPerDay(
        IReadOnlyList<WorkSchedule> schedules,
        int employeeId,
        DateTime date,
        ref bool usedDefault)
    {
        WorkSchedule? schedule = schedules
            .Where(s => s.EmployeeId == employeeId
                && s.EffectiveFromDate.Date <= date
                && (s.EffectiveToDate is null || s.EffectiveToDate.Value.Date >= date))
            .OrderByDescending(s => s.EffectiveFromDate)
            .FirstOrDefault();

        if (schedule is null || schedule.MaxOrdinaryHoursPerDay <= 0m)
        {
            usedDefault = true;
            return DefaultOrdinaryHoursPerDay;
        }

        return schedule.MaxOrdinaryHoursPerDay;
    }

    private static decimal MinimumBaseFor(
        IReadOnlyList<ContributionMinimumBase> minimumBases,
        string contributionCode)
    {
        return minimumBases
            .Where(b => b.ContributionCode == contributionCode)
            .Select(b => b.MinimumBaseAmount)
            .DefaultIfEmpty(0m)
            .Max();
    }

    /// <summary>CCSS first, then Banco Popular, then the other institutions.</summary>
    private static int ContributionDisplayOrder(string contributionCode)
    {
        return contributionCode switch
        {
            "SEM" => 0,
            "IVM" => 1,
            "BancoPopularLpt" => 2,
            "BancoPopularEmployerQuota" => 3,
            _ => 10,
        };
    }

    private static string ContributionLabel(ContributionRate rate)
    {
        return rate.ContributionCode switch
        {
            "SEM" => "CCSS Salud (SEM)",
            "IVM" => "CCSS Pensiones (IVM)",
            "BancoPopularLpt" => "Banco Popular (LPT)",
            "BancoPopularEmployerQuota" => "Banco Popular, cuota patronal",
            "FamilyAllowances" => "Asignaciones Familiares (FODESAF)",
            "IMAS" => "IMAS",
            "INA" => "INA",
            "FCL" => "Fondo de Capitalización Laboral (FCL)",
            "OPC" => "Pensión Complementaria (ROP)",
            "INS" => "INS",
            _ => rate.ContributionName,
        };
    }

    private static string DisabilityLabel(ApprovedDisability disability)
    {
        return disability.DisabilityTypeCode switch
        {
            "CommonIllness" => "enfermedad común",
            "OccupationalRisk" => "riesgo del trabajo",
            "Maternity" => "maternidad",
            _ => disability.DisabilityTypeName,
        };
    }

    private static string OvertimeLabel(ApprovedOvertime overtime)
    {
        return overtime.OvertimeRuleCode switch
        {
            "Standard" => "ordinarias",
            "Holiday" => "en feriado",
            _ => overtime.OvertimeRuleName,
        };
    }

    private static DateTime Max(DateTime left, DateTime right) =>
        left > right ? left : right;

    private static DateTime Min(DateTime left, DateTime right) =>
        left < right ? left : right;

    private sealed class SalaryTimeline
    {
        private readonly decimal _fallbackSalary;
        private readonly List<SalarySegment> _segments;

        public SalaryTimeline(
            CalculationEmployee employee,
            IEnumerable<SalarySegment> segments)
        {
            _fallbackSalary = employee.BaseSalary;
            _segments = segments
                .OrderBy(s => s.EffectiveFromDate)
                .ToList();
        }

        public bool UsedFallback { get; private set; }

        public decimal SalaryOn(DateTime day)
        {
            for (int index = _segments.Count - 1; index >= 0; index--)
            {
                SalarySegment segment = _segments[index];

                if (segment.EffectiveFromDate.Date <= day
                    && (segment.EffectiveToDate is null
                        || segment.EffectiveToDate.Value.Date >= day))
                {
                    return segment.BaseSalary;
                }
            }

            UsedFallback = true;
            return _fallbackSalary;
        }
    }
}
