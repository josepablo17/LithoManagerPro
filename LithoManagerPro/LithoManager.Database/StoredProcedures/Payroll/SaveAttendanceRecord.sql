CREATE PROCEDURE [Payroll].[SaveAttendanceRecord]
    @EmployeeId int,
    @AttendanceDate date,
    @AttendanceStatus nvarchar(4000),
    @ExpectedHours decimal(5,2),
    @WorkedHours decimal(5,2),
    @PaidHours decimal(5,2),
    @UnpaidHours decimal(5,2),
    @WorkShiftTypeId int = NULL,
    @IsPaidHoliday bit = 0,
    @IsApproved bit = 0,
    @Notes nvarchar(4000) = NULL,
    @ExpectedRowVersion varbinary(8) = NULL,
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

    DECLARE @NormalizedAttendanceStatus nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@AttendanceStatus)), N'');

    DECLARE @NormalizedNotes nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@Notes)), N'');

    IF @EmployeeId IS NULL OR @EmployeeId <= 0
    BEGIN
        THROW 56201,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @AttendanceDate IS NULL
    BEGIN
        THROW 56202,
            N'AttendanceDate is required.',
            1;
    END;

    IF @NormalizedAttendanceStatus NOT IN
    (
        N'Present',
        N'Partial',
        N'Absent',
        N'Holiday',
        N'Leave',
        N'Disability'
    )
    BEGIN
        THROW 56203,
            N'AttendanceStatus is invalid.',
            1;
    END;

    IF @ExpectedHours IS NULL
       OR @WorkedHours IS NULL
       OR @PaidHours IS NULL
       OR @UnpaidHours IS NULL
       OR @ExpectedHours < 0
       OR @WorkedHours < 0
       OR @PaidHours < 0
       OR @UnpaidHours < 0
       OR @ExpectedHours > 24
       OR @WorkedHours > 24
       OR @PaidHours > 24
       OR @UnpaidHours > 24
    BEGIN
        THROW 56204,
            N'Attendance hours must be between zero and twenty-four.',
            1;
    END;

    IF @NormalizedNotes IS NOT NULL
       AND LEN(@NormalizedNotes) > 500
    BEGIN
        THROW 56205,
            N'Notes cannot exceed 500 characters.',
            1;
    END;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 56206,
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
    DECLARE @ResolvedWorkShiftTypeId int;
    DECLARE @MaxTotalHoursPerDay decimal(5,2);
    DECLARE @AttendanceRecordId int;
    DECLARE @ExistingRowVersion varbinary(8);
    DECLARE @PreviousValuesJson nvarchar(max);

    DECLARE @ResultAttendance TABLE
    (
        [AttendanceRecordId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [WorkShiftTypeId] int NOT NULL,
        [AttendanceDate] date NOT NULL,
        [AttendanceStatus] nvarchar(30) NOT NULL,
        [ExpectedHours] decimal(5,2) NOT NULL,
        [WorkedHours] decimal(5,2) NOT NULL,
        [PaidHours] decimal(5,2) NOT NULL,
        [UnpaidHours] decimal(5,2) NOT NULL,
        [IsPaidHoliday] bit NOT NULL,
        [IsApproved] bit NOT NULL,
        [ApprovedAtUtc] datetime2(3) NULL,
        [ApprovedByUserId] int NULL,
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
            THROW 56207,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
        BEGIN
            THROW 56208,
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
            THROW 56209,
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
            THROW 56210,
                N'The actor role is not allowed to save attendance.',
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
            THROW 56211,
                N'The employee was not found.',
                1;
        END;

        IF @IsEmployeeActive <> 1
           OR @IsEmployeeDepartmentActive <> 1
        BEGIN
            THROW 56212,
                N'The employee or department is inactive.',
                1;
        END;

        IF @WorkShiftTypeId IS NULL
        BEGIN
            SELECT TOP (1)
                @ResolvedWorkShiftTypeId =
                    EWS.[WorkShiftTypeId]
            FROM [Payroll].[EmployeeWorkSchedules] AS EWS
            WHERE EWS.[EmployeeId] = @EmployeeId
                AND EWS.[EffectiveFromDate] <= @AttendanceDate
                AND
                (
                    EWS.[EffectiveToDate] IS NULL
                    OR EWS.[EffectiveToDate] >= @AttendanceDate
                )
                AND EWS.[IsActive] = 1
            ORDER BY
                EWS.[EffectiveFromDate] DESC;
        END;
        ELSE
        BEGIN
            SET @ResolvedWorkShiftTypeId =
                @WorkShiftTypeId;
        END;

        IF @ResolvedWorkShiftTypeId IS NULL
        BEGIN
            THROW 56213,
                N'No effective work schedule or work shift type was found.',
                1;
        END;

        SELECT
            @MaxTotalHoursPerDay =
                WST.[MaxTotalHoursPerDay]
        FROM [Payroll].[WorkShiftTypes] AS WST
            WITH (UPDLOCK, HOLDLOCK)
        WHERE WST.[WorkShiftTypeId] = @ResolvedWorkShiftTypeId
            AND WST.[IsActive] = 1
            AND WST.[EffectiveFromDate] <= @AttendanceDate
            AND
            (
                WST.[EffectiveToDate] IS NULL
                OR WST.[EffectiveToDate] >= @AttendanceDate
            );

        IF @MaxTotalHoursPerDay IS NULL
        BEGIN
            THROW 56214,
                N'The work shift type was not found or is not effective.',
                1;
        END;

        IF @WorkedHours > @MaxTotalHoursPerDay
        BEGIN
            THROW 56215,
                N'WorkedHours exceeds the selected shift total limit.',
                1;
        END;

        SELECT
            @AttendanceRecordId =
                AR.[AttendanceRecordId],
            @ExistingRowVersion =
                AR.[RowVersion],
            @PreviousValuesJson =
            (
                SELECT
                    AR.[AttendanceRecordId],
                    AR.[EmployeeId],
                    AR.[WorkShiftTypeId],
                    AR.[AttendanceDate],
                    AR.[AttendanceStatus],
                    AR.[ExpectedHours],
                    AR.[WorkedHours],
                    AR.[PaidHours],
                    AR.[UnpaidHours],
                    AR.[IsPaidHoliday],
                    AR.[IsApproved]
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            )
        FROM [Payroll].[AttendanceRecords] AS AR
            WITH (UPDLOCK, HOLDLOCK)
        WHERE AR.[EmployeeId] = @EmployeeId
            AND AR.[AttendanceDate] = @AttendanceDate;

        IF @AttendanceRecordId IS NOT NULL
           AND
           (
               @ExpectedRowVersion IS NULL
               OR DATALENGTH(@ExpectedRowVersion) <> 8
               OR @ExistingRowVersion <> @ExpectedRowVersion
           )
        BEGIN
            THROW 56216,
                N'The attendance record has been modified or requires a valid row version.',
                1;
        END;

        IF @AttendanceRecordId IS NULL
        BEGIN
            INSERT INTO [Payroll].[AttendanceRecords]
            (
                [EmployeeId],
                [WorkShiftTypeId],
                [AttendanceDate],
                [AttendanceStatus],
                [ExpectedHours],
                [WorkedHours],
                [PaidHours],
                [UnpaidHours],
                [IsPaidHoliday],
                [IsApproved],
                [ApprovedAtUtc],
                [ApprovedByUserId],
                [Notes],
                [CreatedByUserId]
            )
            OUTPUT
                INSERTED.[AttendanceRecordId],
                INSERTED.[EmployeeId],
                INSERTED.[WorkShiftTypeId],
                INSERTED.[AttendanceDate],
                INSERTED.[AttendanceStatus],
                INSERTED.[ExpectedHours],
                INSERTED.[WorkedHours],
                INSERTED.[PaidHours],
                INSERTED.[UnpaidHours],
                INSERTED.[IsPaidHoliday],
                INSERTED.[IsApproved],
                INSERTED.[ApprovedAtUtc],
                INSERTED.[ApprovedByUserId],
                INSERTED.[Notes],
                INSERTED.[CreatedAtUtc],
                INSERTED.[CreatedByUserId],
                INSERTED.[UpdatedAtUtc],
                INSERTED.[UpdatedByUserId],
                INSERTED.[RowVersion]
            INTO @ResultAttendance
            VALUES
            (
                @EmployeeId,
                @ResolvedWorkShiftTypeId,
                @AttendanceDate,
                @NormalizedAttendanceStatus,
                @ExpectedHours,
                @WorkedHours,
                @PaidHours,
                @UnpaidHours,
                @IsPaidHoliday,
                @IsApproved,
                CASE WHEN @IsApproved = 1 THEN @OccurredAtUtc ELSE NULL END,
                CASE WHEN @IsApproved = 1 THEN @ActorUserId ELSE NULL END,
                @NormalizedNotes,
                @ActorUserId
            );
        END;
        ELSE
        BEGIN
            UPDATE [Payroll].[AttendanceRecords]
            SET
                [WorkShiftTypeId] = @ResolvedWorkShiftTypeId,
                [AttendanceStatus] = @NormalizedAttendanceStatus,
                [ExpectedHours] = @ExpectedHours,
                [WorkedHours] = @WorkedHours,
                [PaidHours] = @PaidHours,
                [UnpaidHours] = @UnpaidHours,
                [IsPaidHoliday] = @IsPaidHoliday,
                [IsApproved] = @IsApproved,
                [ApprovedAtUtc] =
                    CASE WHEN @IsApproved = 1 THEN @OccurredAtUtc ELSE NULL END,
                [ApprovedByUserId] =
                    CASE WHEN @IsApproved = 1 THEN @ActorUserId ELSE NULL END,
                [Notes] = @NormalizedNotes,
                [UpdatedAtUtc] = @OccurredAtUtc,
                [UpdatedByUserId] = @ActorUserId
            OUTPUT
                INSERTED.[AttendanceRecordId],
                INSERTED.[EmployeeId],
                INSERTED.[WorkShiftTypeId],
                INSERTED.[AttendanceDate],
                INSERTED.[AttendanceStatus],
                INSERTED.[ExpectedHours],
                INSERTED.[WorkedHours],
                INSERTED.[PaidHours],
                INSERTED.[UnpaidHours],
                INSERTED.[IsPaidHoliday],
                INSERTED.[IsApproved],
                INSERTED.[ApprovedAtUtc],
                INSERTED.[ApprovedByUserId],
                INSERTED.[Notes],
                INSERTED.[CreatedAtUtc],
                INSERTED.[CreatedByUserId],
                INSERTED.[UpdatedAtUtc],
                INSERTED.[UpdatedByUserId],
                INSERTED.[RowVersion]
            INTO @ResultAttendance
            WHERE [AttendanceRecordId] = @AttendanceRecordId;
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
            N'AttendanceRecordSaved',
            N'AttendanceRecords',
            CONVERT(nvarchar(100), R.[AttendanceRecordId]),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Attendance record saved successfully.',
            @ClientIpAddress,
            @UserAgent,
            CASE WHEN @AttendanceRecordId IS NULL THEN N'POST' ELSE N'PUT' END,
            @RequestPath,
            @PreviousValuesJson,
            (
                SELECT R.*
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        FROM @ResultAttendance AS R;

        COMMIT TRANSACTION;

        SELECT
            R.[AttendanceRecordId],
            R.[EmployeeId],
            E.[IdentificationType],
            E.[IdentificationNumber],
            E.[FirstName],
            E.[LastName],
            R.[WorkShiftTypeId],
            WST.[WorkShiftTypeCode],
            WST.[Name] AS [WorkShiftTypeName],
            R.[AttendanceDate],
            R.[AttendanceStatus],
            R.[ExpectedHours],
            R.[WorkedHours],
            R.[PaidHours],
            R.[UnpaidHours],
            R.[IsPaidHoliday],
            R.[IsApproved],
            R.[ApprovedAtUtc],
            R.[ApprovedByUserId],
            R.[Notes],
            R.[CreatedAtUtc],
            R.[CreatedByUserId],
            R.[UpdatedAtUtc],
            R.[UpdatedByUserId],
            R.[RowVersion]
        FROM @ResultAttendance AS R
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
