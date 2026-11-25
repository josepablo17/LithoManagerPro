-- Returns everything the payroll calculator needs for a regular run, as
-- several result sets read in this order:
--   1. Employees employed at some point during the period
--   2. Salary history segments overlapping the period
--   3. Work schedules (with shift hours) overlapping the period
--   4. Approved overtime inside the period
--   5. Approved disabilities overlapping the period
--   6. Active payroll concepts
--   7. Social contribution rates effective at the period end
--   8. Social contribution minimum bases effective at the period end
--   9. Monthly income tax brackets effective at the period end
CREATE PROCEDURE [Payroll].[GetPayrollCalculationInputs]
    @PayrollRunId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PeriodStartDate date;
    DECLARE @PeriodEndDate date;

    SELECT
        @PeriodStartDate = [PeriodStartDate],
        @PeriodEndDate = [PeriodEndDate]
    FROM [Payroll].[PayrollRuns]
    WHERE [PayrollRunId] = @PayrollRunId
      AND [RunType] = N'Regular';

    DECLARE @Employees TABLE
    (
        [EmployeeId] int NOT NULL PRIMARY KEY
    );

    -- Active employees hired on or before the period end, plus employees
    -- whose termination date falls inside the period (final payroll).
    INSERT INTO @Employees ([EmployeeId])
    SELECT E.[EmployeeId]
    FROM [HumanResources].[Employees] AS E
    WHERE @PeriodStartDate IS NOT NULL
      AND E.[HireDate] <= @PeriodEndDate
      AND
      (
          (E.[IsActive] = 1 AND (E.[TerminationDate] IS NULL OR E.[TerminationDate] >= @PeriodStartDate))
          OR E.[TerminationDate] BETWEEN @PeriodStartDate AND @PeriodEndDate
      );

    -- 1. Employees
    SELECT
        E.[EmployeeId],
        E.[IdentificationNumber],
        E.[FirstName],
        E.[LastName],
        D.[Name] AS [DepartmentName],
        E.[JobTitle],
        E.[HireDate],
        E.[TerminationDate],
        E.[BaseSalary]
    FROM @Employees AS X
    INNER JOIN [HumanResources].[Employees] AS E
        ON E.[EmployeeId] = X.[EmployeeId]
    LEFT JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]
    ORDER BY E.[EmployeeId];

    -- 2. Salary history segments
    SELECT
        SH.[EmployeeId],
        SH.[BaseSalary],
        SH.[EffectiveFromDate],
        SH.[EffectiveToDate]
    FROM [HumanResources].[EmployeeSalaryHistory] AS SH
    INNER JOIN @Employees AS X
        ON X.[EmployeeId] = SH.[EmployeeId]
    WHERE SH.[EffectiveFromDate] <= @PeriodEndDate
      AND (SH.[EffectiveToDate] IS NULL OR SH.[EffectiveToDate] >= @PeriodStartDate)
    ORDER BY SH.[EmployeeId], SH.[EffectiveFromDate];

    -- 3. Work schedules
    SELECT
        WS.[EmployeeId],
        WST.[WorkShiftTypeCode],
        WST.[MaxOrdinaryHoursPerDay],
        WS.[EffectiveFromDate],
        WS.[EffectiveToDate]
    FROM [Payroll].[EmployeeWorkSchedules] AS WS
    INNER JOIN @Employees AS X
        ON X.[EmployeeId] = WS.[EmployeeId]
    INNER JOIN [Payroll].[WorkShiftTypes] AS WST
        ON WST.[WorkShiftTypeId] = WS.[WorkShiftTypeId]
    WHERE WS.[IsActive] = 1
      AND WS.[EffectiveFromDate] <= @PeriodEndDate
      AND (WS.[EffectiveToDate] IS NULL OR WS.[EffectiveToDate] >= @PeriodStartDate)
    ORDER BY WS.[EmployeeId], WS.[EffectiveFromDate];

    -- 4. Approved overtime
    SELECT
        O.[OvertimeRecordId],
        O.[EmployeeId],
        O.[OvertimeDate],
        O.[Hours],
        ORU.[OvertimeRuleCode],
        ORU.[Name] AS [OvertimeRuleName],
        ORU.[HourMultiplier]
    FROM [Payroll].[OvertimeRecords] AS O
    INNER JOIN @Employees AS X
        ON X.[EmployeeId] = O.[EmployeeId]
    INNER JOIN [Payroll].[OvertimeRules] AS ORU
        ON ORU.[OvertimeRuleId] = O.[OvertimeRuleId]
    WHERE O.[ApprovalStatus] = N'Approved'
      AND O.[OvertimeDate] BETWEEN @PeriodStartDate AND @PeriodEndDate
    ORDER BY O.[EmployeeId], O.[OvertimeDate];

    -- 5. Approved disabilities
    SELECT
        ED.[EmployeeDisabilityId],
        ED.[EmployeeId],
        DT.[DisabilityTypeCode],
        DT.[Name] AS [DisabilityTypeName],
        DT.[ReducesWorkedDays],
        ED.[StartDate],
        ED.[EndDate],
        ED.[EmployerPaidAmount]
    FROM [Payroll].[EmployeeDisabilities] AS ED
    INNER JOIN @Employees AS X
        ON X.[EmployeeId] = ED.[EmployeeId]
    INNER JOIN [Payroll].[DisabilityTypes] AS DT
        ON DT.[DisabilityTypeId] = ED.[DisabilityTypeId]
    WHERE ED.[DisabilityStatus] = N'Approved'
      AND ED.[StartDate] <= @PeriodEndDate
      AND ED.[EndDate] >= @PeriodStartDate
    ORDER BY ED.[EmployeeId], ED.[StartDate];

    -- 6. Concepts
    SELECT
        PC.[PayrollConceptId],
        PC.[PayrollConceptCode],
        PC.[Name],
        PC.[ConceptKind],
        PC.[IsTaxableForIncomeTax],
        PC.[IsSubjectToSocialContributions],
        PC.[CountsForAguinaldo]
    FROM [Payroll].[PayrollConcepts] AS PC
    WHERE PC.[IsActive] = 1;

    -- 7. Contribution rates
    SELECT
        SCT.[ContributionCode],
        SCT.[Name] AS [ContributionName],
        SCT.[AppliesToEmployee],
        SCT.[AppliesToEmployer],
        SCT.[UsesMinimumBase],
        SCR.[EmployeeRate],
        SCR.[EmployerRate]
    FROM [Payroll].[SocialContributionRates] AS SCR
    INNER JOIN [Payroll].[SocialContributionTypes] AS SCT
        ON SCT.[SocialContributionTypeId] = SCR.[SocialContributionTypeId]
    WHERE @PeriodEndDate IS NOT NULL
      AND SCT.[IsActive] = 1
      AND SCR.[EffectiveFromDate] <= @PeriodEndDate
      AND (SCR.[EffectiveToDate] IS NULL OR SCR.[EffectiveToDate] >= @PeriodEndDate)
    ORDER BY SCT.[ContributionCode];

    -- 8. Minimum contribution bases
    SELECT
        SCT.[ContributionCode],
        MB.[MinimumBaseAmount]
    FROM [Payroll].[SocialContributionMinimumBases] AS MB
    INNER JOIN [Payroll].[SocialContributionTypes] AS SCT
        ON SCT.[SocialContributionTypeId] = MB.[SocialContributionTypeId]
    WHERE @PeriodEndDate IS NOT NULL
      AND MB.[EffectiveFromDate] <= @PeriodEndDate
      AND (MB.[EffectiveToDate] IS NULL OR MB.[EffectiveToDate] >= @PeriodEndDate);

    -- 9. Income tax brackets
    SELECT
        ITB.[LowerBoundAmount],
        ITB.[UpperBoundAmount],
        ITB.[TaxRate]
    FROM [Payroll].[IncomeTaxBrackets] AS ITB
    WHERE @PeriodEndDate IS NOT NULL
      AND ITB.[TaxYear] = YEAR(@PeriodEndDate)
      AND ITB.[Periodicity] = N'Monthly'
      AND ITB.[EffectiveFromDate] <= @PeriodEndDate
      AND (ITB.[EffectiveToDate] IS NULL OR ITB.[EffectiveToDate] >= @PeriodEndDate)
    ORDER BY ITB.[LowerBoundAmount];
END;
GO
