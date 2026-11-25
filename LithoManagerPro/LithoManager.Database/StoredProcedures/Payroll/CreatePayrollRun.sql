-- Creates an open payroll run.
--   Regular:   the calendar month PeriodYear/PeriodMonth.
--   Aguinaldo: the window defined by the active aguinaldo rule
--              (1 December of the previous year to 30 November).
-- PaymentDate defaults to the last day of the month (Regular) or to the
-- rule's legal due date (Aguinaldo).
CREATE PROCEDURE [Payroll].[CreatePayrollRun]
    @RunType nvarchar(20),
    @PeriodYear smallint,
    @PeriodMonth tinyint = NULL,
    @PaymentDate date = NULL,
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

    DECLARE @NormalizedRunType nvarchar(20) =
        NULLIF(LTRIM(RTRIM(@RunType)), N'');

    DECLARE @ActorEmailAddress nvarchar(254);
    DECLARE @ActorRoleCode nvarchar(50);
    DECLARE @PeriodStartDate date;
    DECLARE @PeriodEndDate date;
    DECLARE @ResolvedPaymentDate date;
    DECLARE @PayrollRunId int;

    IF @NormalizedRunType IS NULL
       OR @NormalizedRunType NOT IN (N'Regular', N'Aguinaldo')
    BEGIN
        THROW 58101,
            N'RunType must be Regular or Aguinaldo.',
            1;
    END;

    IF @PeriodYear IS NULL OR @PeriodYear NOT BETWEEN 2000 AND 2100
    BEGIN
        THROW 58102,
            N'PeriodYear must be between 2000 and 2100.',
            1;
    END;

    IF @NormalizedRunType = N'Regular'
       AND (@PeriodMonth IS NULL OR @PeriodMonth NOT BETWEEN 1 AND 12)
    BEGIN
        THROW 58103,
            N'PeriodMonth must be between 1 and 12 for regular payrolls.',
            1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC [Payroll].[AuthorizePayrollRunActor]
            @ActorUserId = @ActorUserId,
            @ErrorNumberBase = 58100,
            @ActorEmailAddress = @ActorEmailAddress OUTPUT,
            @ActorRoleCode = @ActorRoleCode OUTPUT;

        IF @NormalizedRunType = N'Regular'
        BEGIN
            SET @PeriodStartDate =
                DATEFROMPARTS(@PeriodYear, @PeriodMonth, 1);

            SET @PeriodEndDate = EOMONTH(@PeriodStartDate);

            SET @ResolvedPaymentDate =
                COALESCE(@PaymentDate, @PeriodEndDate);
        END
        ELSE
        BEGIN
            SET @PeriodMonth = 12;

            SELECT TOP (1)
                @PeriodStartDate =
                    DATEFROMPARTS(
                        CASE
                            WHEN AR.[CalculationStartMonth]
                                > AR.[CalculationEndMonth]
                            THEN @PeriodYear - 1
                            ELSE @PeriodYear
                        END,
                        AR.[CalculationStartMonth],
                        AR.[CalculationStartDay]),
                @PeriodEndDate =
                    DATEFROMPARTS(
                        @PeriodYear,
                        AR.[CalculationEndMonth],
                        AR.[CalculationEndDay]),
                @ResolvedPaymentDate =
                    COALESCE(
                        @PaymentDate,
                        DATEFROMPARTS(
                            @PeriodYear,
                            AR.[PaymentDueMonth],
                            AR.[PaymentDueDay]))
            FROM [Payroll].[AguinaldoRules] AS AR
            WHERE AR.[IsActive] = 1
              AND AR.[EffectiveFromDate] <=
                  DATEFROMPARTS(@PeriodYear, 12, 31)
              AND
              (
                  AR.[EffectiveToDate] IS NULL
                  OR AR.[EffectiveToDate] >=
                      DATEFROMPARTS(@PeriodYear, 1, 1)
              )
            ORDER BY AR.[EffectiveFromDate] DESC;

            IF @PeriodStartDate IS NULL
            BEGIN
                THROW 58111,
                    N'No active aguinaldo rule is effective for the requested year.',
                    1;
            END;
        END;

        IF @ResolvedPaymentDate < @PeriodStartDate
        BEGIN
            THROW 58104,
                N'PaymentDate cannot be earlier than the period start date.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [Payroll].[PayrollRuns] WITH (UPDLOCK, HOLDLOCK)
            WHERE [RunType] = @NormalizedRunType
              AND [PeriodYear] = @PeriodYear
              AND [PeriodMonth] = @PeriodMonth
        )
        BEGIN
            THROW 58110,
                N'A payroll run already exists for this type and period.',
                1;
        END;

        INSERT INTO [Payroll].[PayrollRuns]
        (
            [RunType],
            [PeriodYear],
            [PeriodMonth],
            [PeriodStartDate],
            [PeriodEndDate],
            [PaymentDate],
            [Status],
            [CreatedAtUtc],
            [CreatedByUserId]
        )
        VALUES
        (
            @NormalizedRunType,
            @PeriodYear,
            @PeriodMonth,
            @PeriodStartDate,
            @PeriodEndDate,
            @ResolvedPaymentDate,
            N'Open',
            @OccurredAtUtc,
            @ActorUserId
        );

        SET @PayrollRunId = SCOPE_IDENTITY();

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
            N'PayrollRunCreated',
            N'PayrollRuns',
            CONVERT(nvarchar(100), PR.[PayrollRunId]),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Payroll run created successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            NULL,
            (
                SELECT
                    PR.[PayrollRunId],
                    PR.[RunType],
                    PR.[PeriodYear],
                    PR.[PeriodMonth],
                    PR.[PeriodStartDate],
                    PR.[PeriodEndDate],
                    PR.[PaymentDate],
                    PR.[Status]
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
