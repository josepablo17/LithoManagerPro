-- Closes a calculated payroll run. A closed run is final: it cannot be
-- recalculated, and its lines become the source for the aguinaldo.
CREATE PROCEDURE [Payroll].[ClosePayrollRun]
    @PayrollRunId int,
    @ExpectedRowVersion varbinary(8),
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

    IF @PayrollRunId IS NULL OR @PayrollRunId <= 0
    BEGIN
        THROW 58301,
            N'PayrollRunId must be greater than zero.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
    BEGIN
        THROW 58302,
            N'ExpectedRowVersion is required.',
            1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        EXEC [Payroll].[AuthorizePayrollRunActor]
            @ActorUserId = @ActorUserId,
            @ErrorNumberBase = 58300,
            @ActorEmailAddress = @ActorEmailAddress OUTPUT,
            @ActorRoleCode = @ActorRoleCode OUTPUT;

        SELECT
            @CurrentStatus = [Status],
            @CurrentRowVersion = [RowVersion]
        FROM [Payroll].[PayrollRuns] WITH (UPDLOCK, HOLDLOCK)
        WHERE [PayrollRunId] = @PayrollRunId;

        IF @CurrentStatus IS NULL
        BEGIN
            THROW 58310,
                N'The payroll run was not found.',
                1;
        END;

        IF @CurrentStatus <> N'Calculated'
        BEGIN
            THROW 58311,
                N'Only calculated payroll runs can be closed.',
                1;
        END;

        IF @CurrentRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 58312,
                N'The payroll run was modified by another operation.',
                1;
        END;

        UPDATE [Payroll].[PayrollRuns]
        SET
            [Status] = N'Closed',
            [ClosedAtUtc] = @OccurredAtUtc,
            [ClosedByUserId] = @ActorUserId,
            [UpdatedAtUtc] = @OccurredAtUtc,
            [UpdatedByUserId] = @ActorUserId
        WHERE [PayrollRunId] = @PayrollRunId;

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
            N'PayrollRunClosed',
            N'PayrollRuns',
            CONVERT(nvarchar(100), PR.[PayrollRunId]),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Payroll run closed successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'PATCH',
            @RequestPath,
            N'{"Status":"Calculated"}',
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
