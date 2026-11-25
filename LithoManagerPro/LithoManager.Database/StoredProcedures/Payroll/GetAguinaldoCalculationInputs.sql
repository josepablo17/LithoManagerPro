-- Returns the inputs for an aguinaldo run, read in this order:
--   1. Employees with at least one closed regular payroll in the window
--      (identification and salary from the most recent of those runs)
--   2. Lines from closed regular payrolls whose period starts inside the
--      aguinaldo window (PeriodStartDate..PeriodEndDate of the run)
--   3. Closed regular payroll periods in the window (to detect gaps)
--   4. The active aguinaldo rule
--   5. Disability types
--   6. Active payroll concepts
CREATE PROCEDURE [Payroll].[GetAguinaldoCalculationInputs]
    @PayrollRunId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @WindowStartDate date;
    DECLARE @WindowEndDate date;

    SELECT
        @WindowStartDate = [PeriodStartDate],
        @WindowEndDate = [PeriodEndDate]
    FROM [Payroll].[PayrollRuns]
    WHERE [PayrollRunId] = @PayrollRunId
      AND [RunType] = N'Aguinaldo';

    DECLARE @SourceRuns TABLE
    (
        [PayrollRunId] int NOT NULL PRIMARY KEY,
        [PeriodYear] smallint NOT NULL,
        [PeriodMonth] tinyint NOT NULL,
        [PeriodStartDate] date NOT NULL
    );

    INSERT INTO @SourceRuns
    SELECT
        PR.[PayrollRunId],
        PR.[PeriodYear],
        PR.[PeriodMonth],
        PR.[PeriodStartDate]
    FROM [Payroll].[PayrollRuns] AS PR
    WHERE @WindowStartDate IS NOT NULL
      AND PR.[RunType] = N'Regular'
      AND PR.[Status] = N'Closed'
      AND PR.[PeriodStartDate] BETWEEN @WindowStartDate AND @WindowEndDate;

    -- 1. Employees (latest snapshot inside the window)
    SELECT
        Latest.[EmployeeId],
        Latest.[IdentificationNumber],
        Latest.[EmployeeName],
        Latest.[DepartmentName],
        Latest.[JobTitle],
        Latest.[MonthlyBaseSalary]
    FROM
    (
        SELECT
            PRE.[EmployeeId],
            PRE.[IdentificationNumber],
            PRE.[EmployeeName],
            PRE.[DepartmentName],
            PRE.[JobTitle],
            PRE.[MonthlyBaseSalary],
            ROW_NUMBER() OVER
            (
                PARTITION BY PRE.[EmployeeId]
                ORDER BY SR.[PeriodStartDate] DESC
            ) AS [RowNumber]
        FROM [Payroll].[PayrollRunEmployees] AS PRE
        INNER JOIN @SourceRuns AS SR
            ON SR.[PayrollRunId] = PRE.[PayrollRunId]
    ) AS Latest
    WHERE Latest.[RowNumber] = 1
    ORDER BY Latest.[EmployeeId];

    -- 2. Source lines
    SELECT
        PRE.[EmployeeId],
        SR.[PeriodYear],
        SR.[PeriodMonth],
        PC.[PayrollConceptCode],
        PRL.[ReferenceCode],
        PRL.[Amount]
    FROM [Payroll].[PayrollRunLines] AS PRL
    INNER JOIN [Payroll].[PayrollRunEmployees] AS PRE
        ON PRE.[PayrollRunEmployeeId] = PRL.[PayrollRunEmployeeId]
    INNER JOIN @SourceRuns AS SR
        ON SR.[PayrollRunId] = PRE.[PayrollRunId]
    INNER JOIN [Payroll].[PayrollConcepts] AS PC
        ON PC.[PayrollConceptId] = PRL.[PayrollConceptId]
    WHERE PC.[ConceptKind] = N'Earning'
    ORDER BY PRE.[EmployeeId], SR.[PeriodStartDate], PRL.[SortOrder];

    -- 3. Closed periods in the window
    SELECT
        SR.[PeriodYear],
        SR.[PeriodMonth]
    FROM @SourceRuns AS SR
    ORDER BY SR.[PeriodStartDate];

    -- 4. Aguinaldo rule
    SELECT TOP (1)
        AR.[AguinaldoRuleCode],
        AR.[Divisor],
        AR.[IncludesOrdinarySalary],
        AR.[IncludesOvertime],
        AR.[IncludesSalaryInKind],
        AR.[ExcludesCommonIllnessSubsidy],
        AR.[IncludesMaternitySubsidy]
    FROM [Payroll].[AguinaldoRules] AS AR
    WHERE @WindowEndDate IS NOT NULL
      AND AR.[IsActive] = 1
      AND AR.[EffectiveFromDate] <= @WindowEndDate
      AND (AR.[EffectiveToDate] IS NULL OR AR.[EffectiveToDate] >= @WindowEndDate)
    ORDER BY AR.[EffectiveFromDate] DESC;

    -- 5. Disability types
    SELECT
        DT.[DisabilityTypeCode],
        DT.[CountsAsSalaryForAguinaldo]
    FROM [Payroll].[DisabilityTypes] AS DT;

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
END;
GO
