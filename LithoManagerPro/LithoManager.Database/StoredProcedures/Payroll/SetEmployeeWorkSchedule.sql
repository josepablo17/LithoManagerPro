CREATE PROCEDURE [Payroll].[SetEmployeeWorkSchedule]
    @EmployeeId int,
    @WorkShiftTypeId int,
    @WeeklyOrdinaryHours decimal(5,2),
    @WorksMonday bit = 1,
    @WorksTuesday bit = 1,
    @WorksWednesday bit = 1,
    @WorksThursday bit = 1,
    @WorksFriday bit = 1,
    @WorksSaturday bit = 0,
    @WorksSunday bit = 0,
    @EffectiveFromDate date,
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

    IF @EmployeeId IS NULL OR @EmployeeId <= 0
    BEGIN
        THROW 56101,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @WorkShiftTypeId IS NULL OR @WorkShiftTypeId <= 0
    BEGIN
        THROW 56102,
            N'WorkShiftTypeId must be greater than zero.',
            1;
    END;

    IF @WeeklyOrdinaryHours IS NULL
       OR @WeeklyOrdinaryHours <= 0
    BEGIN
        THROW 56103,
            N'WeeklyOrdinaryHours must be greater than zero.',
            1;
    END;

    IF @EffectiveFromDate IS NULL
    BEGIN
        THROW 56104,
            N'EffectiveFromDate is required.',
            1;
    END;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 56105,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @WorksMonday = 0
       AND @WorksTuesday = 0
       AND @WorksWednesday = 0
       AND @WorksThursday = 0
       AND @WorksFriday = 0
       AND @WorksSaturday = 0
       AND @WorksSunday = 0
    BEGIN
        THROW 56106,
            N'At least one work day is required.',
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
    DECLARE @MaxOrdinaryHoursPerWeek decimal(5,2);
    DECLARE @ExistingScheduleId int;
    DECLARE @ExistingEffectiveFromDate date;
    DECLARE @PreviousValuesJson nvarchar(max);

    DECLARE @ResultSchedule TABLE
    (
        [EmployeeWorkScheduleId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [WorkShiftTypeId] int NOT NULL,
        [WeeklyOrdinaryHours] decimal(5,2) NOT NULL,
        [WorksMonday] bit NOT NULL,
        [WorksTuesday] bit NOT NULL,
        [WorksWednesday] bit NOT NULL,
        [WorksThursday] bit NOT NULL,
        [WorksFriday] bit NOT NULL,
        [WorksSaturday] bit NOT NULL,
        [WorksSunday] bit NOT NULL,
        [EffectiveFromDate] date NOT NULL,
        [EffectiveToDate] date NULL,
        [IsActive] bit NOT NULL,
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
            THROW 56107,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
        BEGIN
            THROW 56108,
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
            THROW 56109,
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
            THROW 56110,
                N'The actor role is not allowed to set work schedules.',
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
            THROW 56111,
                N'The employee was not found.',
                1;
        END;

        IF @IsEmployeeActive <> 1
           OR @IsEmployeeDepartmentActive <> 1
        BEGIN
            THROW 56112,
                N'The employee or department is inactive.',
                1;
        END;

        SELECT
            @MaxOrdinaryHoursPerWeek =
                WST.[MaxOrdinaryHoursPerWeek]
        FROM [Payroll].[WorkShiftTypes] AS WST
            WITH (UPDLOCK, HOLDLOCK)
        WHERE WST.[WorkShiftTypeId] = @WorkShiftTypeId
            AND WST.[IsActive] = 1
            AND WST.[EffectiveFromDate] <= @EffectiveFromDate
            AND
            (
                WST.[EffectiveToDate] IS NULL
                OR WST.[EffectiveToDate] >= @EffectiveFromDate
            );

        IF @MaxOrdinaryHoursPerWeek IS NULL
        BEGIN
            THROW 56113,
                N'The work shift type was not found or is not effective.',
                1;
        END;

        IF @WeeklyOrdinaryHours > @MaxOrdinaryHoursPerWeek
        BEGIN
            THROW 56114,
                N'WeeklyOrdinaryHours exceeds the selected shift limit.',
                1;
        END;

        SELECT
            @ExistingScheduleId =
                EWS.[EmployeeWorkScheduleId],
            @ExistingEffectiveFromDate =
                EWS.[EffectiveFromDate],
            @PreviousValuesJson =
            (
                SELECT
                    EWS.[EmployeeWorkScheduleId],
                    EWS.[EmployeeId],
                    EWS.[WorkShiftTypeId],
                    EWS.[WeeklyOrdinaryHours],
                    EWS.[EffectiveFromDate],
                    EWS.[EffectiveToDate],
                    EWS.[IsActive]
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            )
        FROM [Payroll].[EmployeeWorkSchedules] AS EWS
            WITH (UPDLOCK, HOLDLOCK)
        WHERE EWS.[EmployeeId] = @EmployeeId
            AND EWS.[EffectiveToDate] IS NULL;

        IF @ExistingScheduleId IS NOT NULL
           AND @ExistingEffectiveFromDate > @EffectiveFromDate
        BEGIN
            THROW 56115,
                N'EffectiveFromDate cannot be earlier than the current schedule.',
                1;
        END;

        IF @ExistingScheduleId IS NOT NULL
           AND @ExistingEffectiveFromDate = @EffectiveFromDate
        BEGIN
            UPDATE [Payroll].[EmployeeWorkSchedules]
            SET
                [WorkShiftTypeId] = @WorkShiftTypeId,
                [WeeklyOrdinaryHours] = @WeeklyOrdinaryHours,
                [WorksMonday] = @WorksMonday,
                [WorksTuesday] = @WorksTuesday,
                [WorksWednesday] = @WorksWednesday,
                [WorksThursday] = @WorksThursday,
                [WorksFriday] = @WorksFriday,
                [WorksSaturday] = @WorksSaturday,
                [WorksSunday] = @WorksSunday,
                [IsActive] = 1,
                [UpdatedAtUtc] = @OccurredAtUtc,
                [UpdatedByUserId] = @ActorUserId
            OUTPUT
                INSERTED.[EmployeeWorkScheduleId],
                INSERTED.[EmployeeId],
                INSERTED.[WorkShiftTypeId],
                INSERTED.[WeeklyOrdinaryHours],
                INSERTED.[WorksMonday],
                INSERTED.[WorksTuesday],
                INSERTED.[WorksWednesday],
                INSERTED.[WorksThursday],
                INSERTED.[WorksFriday],
                INSERTED.[WorksSaturday],
                INSERTED.[WorksSunday],
                INSERTED.[EffectiveFromDate],
                INSERTED.[EffectiveToDate],
                INSERTED.[IsActive],
                INSERTED.[CreatedAtUtc],
                INSERTED.[CreatedByUserId],
                INSERTED.[UpdatedAtUtc],
                INSERTED.[UpdatedByUserId],
                INSERTED.[RowVersion]
            INTO @ResultSchedule
            WHERE [EmployeeWorkScheduleId] = @ExistingScheduleId;
        END;
        ELSE
        BEGIN
            IF @ExistingScheduleId IS NOT NULL
            BEGIN
                UPDATE [Payroll].[EmployeeWorkSchedules]
                SET
                    [EffectiveToDate] =
                        DATEADD(DAY, -1, @EffectiveFromDate),
                    [IsActive] = 0,
                    [UpdatedAtUtc] = @OccurredAtUtc,
                    [UpdatedByUserId] = @ActorUserId
                WHERE [EmployeeWorkScheduleId] =
                    @ExistingScheduleId;
            END;

            INSERT INTO [Payroll].[EmployeeWorkSchedules]
            (
                [EmployeeId],
                [WorkShiftTypeId],
                [WeeklyOrdinaryHours],
                [WorksMonday],
                [WorksTuesday],
                [WorksWednesday],
                [WorksThursday],
                [WorksFriday],
                [WorksSaturday],
                [WorksSunday],
                [EffectiveFromDate],
                [CreatedByUserId]
            )
            OUTPUT
                INSERTED.[EmployeeWorkScheduleId],
                INSERTED.[EmployeeId],
                INSERTED.[WorkShiftTypeId],
                INSERTED.[WeeklyOrdinaryHours],
                INSERTED.[WorksMonday],
                INSERTED.[WorksTuesday],
                INSERTED.[WorksWednesday],
                INSERTED.[WorksThursday],
                INSERTED.[WorksFriday],
                INSERTED.[WorksSaturday],
                INSERTED.[WorksSunday],
                INSERTED.[EffectiveFromDate],
                INSERTED.[EffectiveToDate],
                INSERTED.[IsActive],
                INSERTED.[CreatedAtUtc],
                INSERTED.[CreatedByUserId],
                INSERTED.[UpdatedAtUtc],
                INSERTED.[UpdatedByUserId],
                INSERTED.[RowVersion]
            INTO @ResultSchedule
            VALUES
            (
                @EmployeeId,
                @WorkShiftTypeId,
                @WeeklyOrdinaryHours,
                @WorksMonday,
                @WorksTuesday,
                @WorksWednesday,
                @WorksThursday,
                @WorksFriday,
                @WorksSaturday,
                @WorksSunday,
                @EffectiveFromDate,
                @ActorUserId
            );
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
            N'EmployeeWorkScheduleSet',
            N'EmployeeWorkSchedules',
            CONVERT(nvarchar(100), R.[EmployeeWorkScheduleId]),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Employee work schedule set successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            @PreviousValuesJson,
            (
                SELECT R.*
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        FROM @ResultSchedule AS R;

        COMMIT TRANSACTION;

        SELECT
            R.[EmployeeWorkScheduleId],
            R.[EmployeeId],
            E.[IdentificationType],
            E.[IdentificationNumber],
            E.[FirstName],
            E.[LastName],
            R.[WorkShiftTypeId],
            WST.[WorkShiftTypeCode],
            WST.[Name] AS [WorkShiftTypeName],
            R.[WeeklyOrdinaryHours],
            R.[WorksMonday],
            R.[WorksTuesday],
            R.[WorksWednesday],
            R.[WorksThursday],
            R.[WorksFriday],
            R.[WorksSaturday],
            R.[WorksSunday],
            R.[EffectiveFromDate],
            R.[EffectiveToDate],
            R.[IsActive],
            R.[CreatedAtUtc],
            R.[CreatedByUserId],
            R.[UpdatedAtUtc],
            R.[UpdatedByUserId],
            R.[RowVersion]
        FROM @ResultSchedule AS R
        INNER JOIN [HumanResources].[Employees] AS E
            ON E.[EmployeeId] = R.[EmployeeId]
        INNER JOIN [Payroll].[WorkShiftTypes] AS WST
            ON WST.[WorkShiftTypeId] = R.[WorkShiftTypeId];
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
