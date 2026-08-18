CREATE PROCEDURE [Payroll].[CreateOvertimeRecord]
    @EmployeeId int,
    @OvertimeRuleId int,
    @OvertimeDate date,
    @Hours decimal(5,2),
    @AttendanceRecordId int = NULL,
    @Notes nvarchar(4000) = NULL,
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

    DECLARE @NormalizedNotes nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@Notes)), N'');

    IF @EmployeeId IS NULL OR @EmployeeId <= 0
    BEGIN
        THROW 56301,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @OvertimeRuleId IS NULL OR @OvertimeRuleId <= 0
    BEGIN
        THROW 56302,
            N'OvertimeRuleId must be greater than zero.',
            1;
    END;

    IF @OvertimeDate IS NULL
    BEGIN
        THROW 56303,
            N'OvertimeDate is required.',
            1;
    END;

    IF @Hours IS NULL OR @Hours <= 0 OR @Hours > 24
    BEGIN
        THROW 56304,
            N'Hours must be greater than zero and less than or equal to twenty-four.',
            1;
    END;

    IF @NormalizedNotes IS NOT NULL
       AND LEN(@NormalizedNotes) > 500
    BEGIN
        THROW 56305,
            N'Notes cannot exceed 500 characters.',
            1;
    END;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 56306,
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
    DECLARE @IsEmployeeActive bit;
    DECLARE @IsEmployeeDepartmentActive bit;
    DECLARE @OvertimeRuleCode nvarchar(50);
    DECLARE @OvertimeRuleName nvarchar(100);
    DECLARE @HourMultiplier decimal(9,4);
    DECLARE @IsOvertimeRuleActive bit;
    DECLARE @OvertimeRuleEffectiveFromDate date;
    DECLARE @OvertimeRuleEffectiveToDate date;
    DECLARE @AttendanceEmployeeId int;
    DECLARE @AttendanceDate date;

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
            THROW 56307,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
        BEGIN
            THROW 56308,
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
            THROW 56309,
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
            THROW 56310,
                N'The actor role is not allowed to create overtime records.',
                1;
        END;

        SELECT
            @IsEmployeeActive = E.[IsActive],
            @IsEmployeeDepartmentActive = D.[IsActive]
        FROM [HumanResources].[Employees] AS E WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [HumanResources].[Departments] AS D
            ON D.[DepartmentId] = E.[DepartmentId]
        WHERE E.[EmployeeId] = @EmployeeId;

        IF @IsEmployeeActive IS NULL
        BEGIN
            THROW 56311,
                N'The employee was not found.',
                1;
        END;

        IF @IsEmployeeActive <> 1
           OR @IsEmployeeDepartmentActive <> 1
        BEGIN
            THROW 56312,
                N'The employee or department is inactive.',
                1;
        END;

        SELECT
            @OvertimeRuleCode = [OvertimeRuleCode],
            @OvertimeRuleName = [Name],
            @HourMultiplier = [HourMultiplier],
            @IsOvertimeRuleActive = [IsActive],
            @OvertimeRuleEffectiveFromDate = [EffectiveFromDate],
            @OvertimeRuleEffectiveToDate = [EffectiveToDate]
        FROM [Payroll].[OvertimeRules] WITH (UPDLOCK, HOLDLOCK)
        WHERE [OvertimeRuleId] = @OvertimeRuleId;

        IF @OvertimeRuleCode IS NULL
        BEGIN
            THROW 56313,
                N'The overtime rule was not found.',
                1;
        END;

        IF @IsOvertimeRuleActive <> 1
           OR @OvertimeDate < @OvertimeRuleEffectiveFromDate
           OR
           (
               @OvertimeRuleEffectiveToDate IS NOT NULL
               AND @OvertimeDate > @OvertimeRuleEffectiveToDate
           )
        BEGIN
            THROW 56314,
                N'The overtime rule is inactive or not effective on the overtime date.',
                1;
        END;

        IF @AttendanceRecordId IS NOT NULL
        BEGIN
            SELECT
                @AttendanceEmployeeId = [EmployeeId],
                @AttendanceDate = [AttendanceDate]
            FROM [Payroll].[AttendanceRecords] WITH (UPDLOCK, HOLDLOCK)
            WHERE [AttendanceRecordId] = @AttendanceRecordId;

            IF @AttendanceEmployeeId IS NULL
            BEGIN
                THROW 56315,
                    N'The attendance record was not found.',
                    1;
            END;

            IF @AttendanceEmployeeId <> @EmployeeId
               OR @AttendanceDate <> @OvertimeDate
            BEGIN
                THROW 56316,
                    N'The attendance record does not match the employee and overtime date.',
                    1;
            END;
        END;

        INSERT INTO [Payroll].[OvertimeRecords]
        (
            [EmployeeId],
            [AttendanceRecordId],
            [OvertimeRuleId],
            [OvertimeDate],
            [Hours],
            [ApprovalStatus],
            [Notes],
            [CreatedAtUtc],
            [CreatedByUserId]
        )
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
        VALUES
        (
            @EmployeeId,
            @AttendanceRecordId,
            @OvertimeRuleId,
            @OvertimeDate,
            @Hours,
            N'Pending',
            @NormalizedNotes,
            @OccurredAtUtc,
            @ActorUserId
        );

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
            N'OvertimeRecordCreated',
            N'OvertimeRecords',
            CONVERT(nvarchar(100), R.[OvertimeRecordId]),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Overtime record created successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            NULL,
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
