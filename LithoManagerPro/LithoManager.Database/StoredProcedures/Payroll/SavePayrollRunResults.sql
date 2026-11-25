-- Replaces the results of an open or calculated payroll run with the
-- output of the application's payroll calculator and marks the run as
-- Calculated. @ResultsJson has this shape:
--   [
--     {
--       "employeeId": 1, "identificationNumber": "...", "employeeName": "...",
--       "departmentName": "...", "jobTitle": "...", "monthlyBaseSalary": 0,
--       "paidDays": 30, "grossAmount": 0, "employeeContributionsAmount": 0,
--       "incomeTaxAmount": 0, "netAmount": 0, "employerContributionsAmount": 0,
--       "lines": [
--         { "payrollConceptId": 1, "referenceCode": null, "description": "...",
--           "quantity": null, "rate": null, "baseAmount": null,
--           "amount": 0, "sortOrder": 1 }
--       ]
--     }
--   ]
CREATE PROCEDURE [Payroll].[SavePayrollRunResults]
    @PayrollRunId int,
    @ExpectedRowVersion varbinary(8),
    @ResultsJson nvarchar(max),
    @ActorUserId int,
    @CorrelationId uniqueidentifier = NULL,
    @ClientIpAddress nvarchar(45) = NULL,
    @UserAgent nvarchar(512) = NULL,
    @RequestPath nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OccurredAtUtc datetime2(3) =
        SYSUTCDATETIME();

    DECLARE @ResolvedCorrelationId uniqueidentifier =
        COALESCE(@CorrelationId, NEWID());

    DECLARE @ActorEmailAddress nvarchar(254);
    DECLARE @ActorRoleCode nvarchar(50);
    DECLARE @CurrentStatus nvarchar(20);
    DECLARE @CurrentRowVersion varbinary(8);
    DECLARE @PreviousValuesJson nvarchar(max);

    IF @PayrollRunId IS NULL OR @PayrollRunId <= 0
    BEGIN
        THROW 58201,
            N'PayrollRunId must be greater than zero.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
    BEGIN
        THROW 58202,
            N'ExpectedRowVersion is required.',
            1;
    END;

    IF @ResultsJson IS NULL
       OR ISJSON(@ResultsJson) <> 1
       OR LEFT(LTRIM(@ResultsJson), 1) <> N'['
    BEGIN
        THROW 58203,
            N'ResultsJson must be a JSON array.',
            1;
    END;

    DECLARE @Employees TABLE
    (
        [EmployeeId] int NOT NULL PRIMARY KEY,
        [IdentificationNumber] nvarchar(30) NOT NULL,
        [EmployeeName] nvarchar(260) NOT NULL,
        [DepartmentName] nvarchar(100) NULL,
        [JobTitle] nvarchar(100) NULL,
        [MonthlyBaseSalary] decimal(18,2) NOT NULL,
        [PaidDays] decimal(5,2) NOT NULL,
        [GrossAmount] decimal(18,2) NOT NULL,
        [EmployeeContributionsAmount] decimal(18,2) NOT NULL,
        [IncomeTaxAmount] decimal(18,2) NOT NULL,
        [NetAmount] decimal(18,2) NOT NULL,
        [EmployerContributionsAmount] decimal(18,2) NOT NULL,
        [LinesJson] nvarchar(max) NULL
    );

    INSERT INTO @Employees
    SELECT
        J.[EmployeeId],
        J.[IdentificationNumber],
        J.[EmployeeName],
        J.[DepartmentName],
        J.[JobTitle],
        J.[MonthlyBaseSalary],
        J.[PaidDays],
        J.[GrossAmount],
        J.[EmployeeContributionsAmount],
        J.[IncomeTaxAmount],
        J.[NetAmount],
        J.[EmployerContributionsAmount],
        J.[LinesJson]
    FROM OPENJSON(@ResultsJson)
    WITH
    (
        [EmployeeId] int N'$.employeeId',
        [IdentificationNumber] nvarchar(30) N'$.identificationNumber',
        [EmployeeName] nvarchar(260) N'$.employeeName',
        [DepartmentName] nvarchar(100) N'$.departmentName',
        [JobTitle] nvarchar(100) N'$.jobTitle',
        [MonthlyBaseSalary] decimal(18,2) N'$.monthlyBaseSalary',
        [PaidDays] decimal(5,2) N'$.paidDays',
        [GrossAmount] decimal(18,2) N'$.grossAmount',
        [EmployeeContributionsAmount] decimal(18,2) N'$.employeeContributionsAmount',
        [IncomeTaxAmount] decimal(18,2) N'$.incomeTaxAmount',
        [NetAmount] decimal(18,2) N'$.netAmount',
        [EmployerContributionsAmount] decimal(18,2) N'$.employerContributionsAmount',
        [LinesJson] nvarchar(max) N'$.lines' AS JSON
    ) AS J;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC [Payroll].[AuthorizePayrollRunActor]
            @ActorUserId = @ActorUserId,
            @ErrorNumberBase = 58200,
            @ActorEmailAddress = @ActorEmailAddress OUTPUT,
            @ActorRoleCode = @ActorRoleCode OUTPUT;

        SELECT
            @CurrentStatus = [Status],
            @CurrentRowVersion = [RowVersion],
            @PreviousValuesJson =
            (
                SELECT
                    PR.[Status],
                    PR.[EmployeeCount],
                    PR.[TotalGrossAmount],
                    PR.[TotalNetAmount]
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            )
        FROM [Payroll].[PayrollRuns] AS PR WITH (UPDLOCK, HOLDLOCK)
        WHERE PR.[PayrollRunId] = @PayrollRunId;

        IF @CurrentStatus IS NULL
        BEGIN
            THROW 58210,
                N'The payroll run was not found.',
                1;
        END;

        IF @CurrentStatus = N'Closed'
        BEGIN
            THROW 58211,
                N'A closed payroll run cannot be recalculated.',
                1;
        END;

        IF @CurrentRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 58212,
                N'The payroll run was modified by another operation.',
                1;
        END;

        DELETE PRL
        FROM [Payroll].[PayrollRunLines] AS PRL
        INNER JOIN [Payroll].[PayrollRunEmployees] AS PRE
            ON PRE.[PayrollRunEmployeeId] = PRL.[PayrollRunEmployeeId]
        WHERE PRE.[PayrollRunId] = @PayrollRunId;

        DELETE FROM [Payroll].[PayrollRunEmployees]
        WHERE [PayrollRunId] = @PayrollRunId;

        INSERT INTO [Payroll].[PayrollRunEmployees]
        (
            [PayrollRunId],
            [EmployeeId],
            [IdentificationNumber],
            [EmployeeName],
            [DepartmentName],
            [JobTitle],
            [MonthlyBaseSalary],
            [PaidDays],
            [GrossAmount],
            [EmployeeContributionsAmount],
            [IncomeTaxAmount],
            [NetAmount],
            [EmployerContributionsAmount]
        )
        SELECT
            @PayrollRunId,
            E.[EmployeeId],
            E.[IdentificationNumber],
            E.[EmployeeName],
            E.[DepartmentName],
            E.[JobTitle],
            E.[MonthlyBaseSalary],
            E.[PaidDays],
            E.[GrossAmount],
            E.[EmployeeContributionsAmount],
            E.[IncomeTaxAmount],
            E.[NetAmount],
            E.[EmployerContributionsAmount]
        FROM @Employees AS E;

        INSERT INTO [Payroll].[PayrollRunLines]
        (
            [PayrollRunEmployeeId],
            [PayrollConceptId],
            [ReferenceCode],
            [Description],
            [Quantity],
            [Rate],
            [BaseAmount],
            [Amount],
            [SortOrder]
        )
        SELECT
            PRE.[PayrollRunEmployeeId],
            L.[PayrollConceptId],
            L.[ReferenceCode],
            L.[Description],
            L.[Quantity],
            L.[Rate],
            L.[BaseAmount],
            L.[Amount],
            L.[SortOrder]
        FROM @Employees AS E
        INNER JOIN [Payroll].[PayrollRunEmployees] AS PRE
            ON PRE.[PayrollRunId] = @PayrollRunId
           AND PRE.[EmployeeId] = E.[EmployeeId]
        CROSS APPLY OPENJSON(E.[LinesJson])
        WITH
        (
            [PayrollConceptId] int N'$.payrollConceptId',
            [ReferenceCode] nvarchar(50) N'$.referenceCode',
            [Description] nvarchar(200) N'$.description',
            [Quantity] decimal(9,2) N'$.quantity',
            [Rate] decimal(18,6) N'$.rate',
            [BaseAmount] decimal(18,2) N'$.baseAmount',
            [Amount] decimal(18,2) N'$.amount',
            [SortOrder] int N'$.sortOrder'
        ) AS L;

        UPDATE PR
        SET
            [Status] = N'Calculated',
            [EmployeeCount] = Totals.[EmployeeCount],
            [TotalGrossAmount] = Totals.[TotalGrossAmount],
            [TotalEmployeeContributionsAmount] = Totals.[TotalEmployeeContributionsAmount],
            [TotalIncomeTaxAmount] = Totals.[TotalIncomeTaxAmount],
            [TotalNetAmount] = Totals.[TotalNetAmount],
            [TotalEmployerContributionsAmount] = Totals.[TotalEmployerContributionsAmount],
            [CalculatedAtUtc] = @OccurredAtUtc,
            [CalculatedByUserId] = @ActorUserId,
            [UpdatedAtUtc] = @OccurredAtUtc,
            [UpdatedByUserId] = @ActorUserId
        FROM [Payroll].[PayrollRuns] AS PR
        CROSS JOIN
        (
            SELECT
                COUNT(*) AS [EmployeeCount],
                COALESCE(SUM([GrossAmount]), 0) AS [TotalGrossAmount],
                COALESCE(SUM([EmployeeContributionsAmount]), 0) AS [TotalEmployeeContributionsAmount],
                COALESCE(SUM([IncomeTaxAmount]), 0) AS [TotalIncomeTaxAmount],
                COALESCE(SUM([NetAmount]), 0) AS [TotalNetAmount],
                COALESCE(SUM([EmployerContributionsAmount]), 0) AS [TotalEmployerContributionsAmount]
            FROM [Payroll].[PayrollRunEmployees]
            WHERE [PayrollRunId] = @PayrollRunId
        ) AS Totals
        WHERE PR.[PayrollRunId] = @PayrollRunId;

        INSERT INTO [Audit].[AuditLogs]
        (
            [CorrelationId],
            [ModuleName],
            [ActionName],
            [EntityName],
            [EntityId],
            [ActorType],
            [ActorUserId],
            [ActorEmailAddress],
            [ActorRoleCode],
            [IsSuccessful],
            [EventDescription],
            [ClientIpAddress],
            [UserAgent],
            [HttpMethod],
            [RequestPath],
            [PreviousValuesJson],
            [NewValuesJson],
            [OccurredAtUtc]
        )
        SELECT
            @ResolvedCorrelationId,
            N'Payroll',
            N'PayrollRunCalculated',
            N'PayrollRuns',
            CONVERT(nvarchar(100), PR.[PayrollRunId]),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Payroll run calculated successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            @PreviousValuesJson,
            (
                SELECT
                    PR.[Status],
                    PR.[EmployeeCount],
                    PR.[TotalGrossAmount],
                    PR.[TotalNetAmount]
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        FROM [Payroll].[PayrollRuns] AS PR
        WHERE PR.[PayrollRunId] = @PayrollRunId;

        COMMIT TRANSACTION;

        EXEC [Payroll].[GetPayrollRunById]
            @PayrollRunId = @PayrollRunId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;
    END CATCH;
END;
GO
