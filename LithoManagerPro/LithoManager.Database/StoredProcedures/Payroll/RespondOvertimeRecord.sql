CREATE PROCEDURE [Payroll].[RespondOvertimeRecord]
    @OvertimeRecordId int,
    @IsApproved bit,
    @RejectionReason nvarchar(4000) = NULL,
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

    DECLARE @NormalizedRejectionReason nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@RejectionReason)), N'');

    IF @OvertimeRecordId IS NULL OR @OvertimeRecordId <= 0
    BEGIN
        THROW 56501,
            N'OvertimeRecordId must be greater than zero.',
            1;
    END;

    IF @IsApproved IS NULL
    BEGIN
        THROW 56502,
            N'IsApproved is required.',
            1;
    END;

    IF @IsApproved = 0
       AND @NormalizedRejectionReason IS NULL
    BEGIN
        THROW 56503,
            N'RejectionReason is required when rejecting overtime.',
            1;
    END;

    IF @NormalizedRejectionReason IS NOT NULL
       AND LEN(@NormalizedRejectionReason) > 300
    BEGIN
        THROW 56504,
            N'RejectionReason cannot exceed 300 characters.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
       OR DATALENGTH(@ExpectedRowVersion) <> 8
    BEGIN
        THROW 56505,
            N'ExpectedRowVersion must contain exactly 8 bytes.',
            1;
    END;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 56506,
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
    DECLARE @NewApprovalStatus nvarchar(30);
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

    SET @NewApprovalStatus =
        CASE
            WHEN @IsApproved = 1 THEN N'Approved'
            ELSE N'Rejected'
        END;

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
            THROW 56507,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
        BEGIN
            THROW 56508,
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
            THROW 56509,
                N'The actor employee or department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator'
        )
        BEGIN
            THROW 56510,
                N'The actor role is not allowed to respond overtime records.',
                1;
        END;

        SELECT
            @CurrentApprovalStatus = OTR.[ApprovalStatus],
            @ExistingRowVersion = OTR.[RowVersion],
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
                    OTR.[ApprovedAtUtc],
                    OTR.[ApprovedByUserId],
                    OTR.[RejectedAtUtc],
                    OTR.[RejectedByUserId],
                    OTR.[RejectionReason],
                    OTR.[Notes]
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            )
        FROM [Payroll].[OvertimeRecords] AS OTR WITH (UPDLOCK, HOLDLOCK)
        WHERE OTR.[OvertimeRecordId] = @OvertimeRecordId;

        IF @CurrentApprovalStatus IS NULL
        BEGIN
            THROW 56511,
                N'The overtime record was not found.',
                1;
        END;

        IF @ExistingRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 56512,
                N'The overtime record has been modified by another transaction.',
                1;
        END;

        IF @CurrentApprovalStatus <> N'Pending'
        BEGIN
            THROW 56513,
                N'Only pending overtime records can be responded.',
                1;
        END;

        UPDATE [Payroll].[OvertimeRecords]
        SET
            [ApprovalStatus] = @NewApprovalStatus,
            [ApprovedAtUtc] =
                CASE WHEN @IsApproved = 1 THEN @OccurredAtUtc ELSE NULL END,
            [ApprovedByUserId] =
                CASE WHEN @IsApproved = 1 THEN @ActorUserId ELSE NULL END,
            [RejectedAtUtc] =
                CASE WHEN @IsApproved = 0 THEN @OccurredAtUtc ELSE NULL END,
            [RejectedByUserId] =
                CASE WHEN @IsApproved = 0 THEN @ActorUserId ELSE NULL END,
            [RejectionReason] =
                CASE
                    WHEN @IsApproved = 0
                        THEN CONVERT(nvarchar(300), @NormalizedRejectionReason)
                    ELSE NULL
                END,
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
            THROW 56514,
                N'The overtime response returned an unexpected row count.',
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
            CASE WHEN @IsApproved = 1 THEN N'OvertimeRecordApproved' ELSE N'OvertimeRecordRejected' END,
            N'OvertimeRecords',
            CONVERT(nvarchar(100), R.[OvertimeRecordId]),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            CASE
                WHEN @IsApproved = 1 THEN N'Overtime record approved successfully.'
                ELSE N'Overtime record rejected successfully.'
            END,
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
