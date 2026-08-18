CREATE PROCEDURE [Payroll].[CancelOvertimeRecord]
    @OvertimeRecordId int,
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

    IF @OvertimeRecordId IS NULL OR @OvertimeRecordId <= 0
    BEGIN
        THROW 56601,
            N'OvertimeRecordId must be greater than zero.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
       OR DATALENGTH(@ExpectedRowVersion) <> 8
    BEGIN
        THROW 56602,
            N'ExpectedRowVersion must contain exactly 8 bytes.',
            1;
    END;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 56603,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    DECLARE @ActorEmailAddress nvarchar(254);
    DECLARE @ActorRoleCode nvarchar(50);
    DECLARE @IsActorUserActive bit;
    DECLARE @IsActorRoleActive bit;
    DECLARE @ActorEmployeeId int;
    DECLARE @IsActorEmployeeActive bit;
    DECLARE @IsActorDepartmentActive bit;
    DECLARE @CurrentApprovalStatus nvarchar(30);
    DECLARE @ExistingRowVersion varbinary(8);
    DECLARE @CreatedByUserId int;
    DECLARE @PreviousValuesJson nvarchar(max);

    DECLARE @ResultOvertime TABLE
    (
        [OvertimeRecordId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [AttendanceRecordId] int NULL,
        [OvertimeRuleId] int NOT NULL,
        [OvertimeDate] date NOT NULL,
        [Hours] decimal(5,2) NOT NULL,
        [ApprovalStatus] nvarchar(30) NOT NULL,
        [ApprovedAtUtc] datetime2(3) NULL,
        [ApprovedByUserId] int NULL,
        [RejectedAtUtc] datetime2(3) NULL,
        [RejectedByUserId] int NULL,
        [RejectionReason] nvarchar(300) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [CreatedByUserId] int NULL,
        [UpdatedAtUtc] datetime2(3) NULL,
        [UpdatedByUserId] int NULL,
        [RowVersion] varbinary(8) NOT NULL
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @ActorEmailAddress = U.[EmailAddress],
            @ActorRoleCode = R.[RoleCode],
            @IsActorUserActive = U.[IsActive],
            @IsActorRoleActive = R.[IsActive],
            @ActorEmployeeId = E.[EmployeeId],
            @IsActorEmployeeActive = E.[IsActive],
            @IsActorDepartmentActive = D.[IsActive]
        FROM [Security].[Users] AS U WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [Security].[Roles] AS R
            ON R.[RoleId] = U.[RoleId]
        LEFT JOIN [HumanResources].[Employees] AS E
            ON E.[UserId] = U.[UserId]
        LEFT JOIN [HumanResources].[Departments] AS D
            ON D.[DepartmentId] = E.[DepartmentId]
        WHERE U.[UserId] = @ActorUserId;

        IF @ActorEmailAddress IS NULL
        BEGIN
            THROW 56604,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
        BEGIN
            THROW 56605,
                N'The actor user or role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND
           (
               @IsActorEmployeeActive <> 1
               OR @IsActorDepartmentActive <> 1
           )
        BEGIN
            THROW 56606,
                N'The actor employee or department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator',
            N'HumanResourcesStaff'
        )
        BEGIN
            THROW 56607,
                N'The actor role is not allowed to cancel overtime records.',
                1;
        END;

        SELECT
            @CurrentApprovalStatus = OTR.[ApprovalStatus],
            @ExistingRowVersion = OTR.[RowVersion],
            @CreatedByUserId = OTR.[CreatedByUserId],
            @PreviousValuesJson =
            (
                SELECT
                    OTR.[OvertimeRecordId],
                    OTR.[EmployeeId],
                    OTR.[AttendanceRecordId],
                    OTR.[OvertimeRuleId],
                    OTR.[OvertimeDate],
                    OTR.[Hours],
                    OTR.[ApprovalStatus],
                    OTR.[Notes]
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            )
        FROM [Payroll].[OvertimeRecords] AS OTR WITH (UPDLOCK, HOLDLOCK)
        WHERE OTR.[OvertimeRecordId] = @OvertimeRecordId;

        IF @CurrentApprovalStatus IS NULL
        BEGIN
            THROW 56608,
                N'The overtime record was not found.',
                1;
        END;

        IF @ExistingRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 56609,
                N'The overtime record has been modified by another transaction.',
                1;
        END;

        IF @CurrentApprovalStatus <> N'Pending'
        BEGIN
            THROW 56610,
                N'Only pending overtime records can be cancelled.',
                1;
        END;

        IF @ActorRoleCode = N'HumanResourcesStaff'
           AND ISNULL(@CreatedByUserId, -1) <> @ActorUserId
        BEGIN
            THROW 56611,
                N'The actor is not allowed to cancel this overtime record.',
                1;
        END;

        UPDATE [Payroll].[OvertimeRecords]
        SET
            [ApprovalStatus] = N'Cancelled',
            [UpdatedAtUtc] = @OccurredAtUtc,
            [UpdatedByUserId] = @ActorUserId
        OUTPUT
            INSERTED.[OvertimeRecordId],
            INSERTED.[EmployeeId],
            INSERTED.[AttendanceRecordId],
            INSERTED.[OvertimeRuleId],
            INSERTED.[OvertimeDate],
            INSERTED.[Hours],
            INSERTED.[ApprovalStatus],
            INSERTED.[ApprovedAtUtc],
            INSERTED.[ApprovedByUserId],
            INSERTED.[RejectedAtUtc],
            INSERTED.[RejectedByUserId],
            INSERTED.[RejectionReason],
            INSERTED.[Notes],
            INSERTED.[CreatedAtUtc],
            INSERTED.[CreatedByUserId],
            INSERTED.[UpdatedAtUtc],
            INSERTED.[UpdatedByUserId],
            INSERTED.[RowVersion]
        INTO @ResultOvertime
        WHERE [OvertimeRecordId] = @OvertimeRecordId;

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 56612,
                N'The overtime cancellation returned an unexpected row count.',
                1;
        END;

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
            N'OvertimeRecordCancelled',
            N'OvertimeRecords',
            CONVERT(nvarchar(100), R.[OvertimeRecordId]),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Overtime record cancelled successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'PATCH',
            @RequestPath,
            @PreviousValuesJson,
            (
                SELECT R.*
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        FROM @ResultOvertime AS R;

        COMMIT TRANSACTION;

        SELECT
            R.[OvertimeRecordId],
            R.[EmployeeId],
            E.[IdentificationType],
            E.[IdentificationNumber],
            E.[FirstName],
            E.[LastName],
            R.[AttendanceRecordId],
            R.[OvertimeRuleId],
            ORU.[OvertimeRuleCode],
            ORU.[Name] AS [OvertimeRuleName],
            ORU.[HourMultiplier],
            R.[OvertimeDate],
            R.[Hours],
            R.[ApprovalStatus],
            R.[ApprovedAtUtc],
            R.[ApprovedByUserId],
            R.[RejectedAtUtc],
            R.[RejectedByUserId],
            R.[RejectionReason],
            R.[Notes],
            R.[CreatedAtUtc],
            R.[CreatedByUserId],
            R.[UpdatedAtUtc],
            R.[UpdatedByUserId],
            R.[RowVersion]
        FROM @ResultOvertime AS R
        INNER JOIN [HumanResources].[Employees] AS E
            ON E.[EmployeeId] = R.[EmployeeId]
        INNER JOIN [Payroll].[OvertimeRules] AS ORU
            ON ORU.[OvertimeRuleId] = R.[OvertimeRuleId];
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
