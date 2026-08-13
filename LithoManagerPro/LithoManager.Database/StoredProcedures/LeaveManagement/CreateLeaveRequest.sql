CREATE PROCEDURE [LeaveManagement].[CreateLeaveRequest]
    @StartDate date,
    @EndDate date,
    @ActorUserId int,
    @LeaveTypeCode nvarchar(4000) = N'Vacation',
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
        COALESCE(
            @CorrelationId,
            NEWID()
        );

    DECLARE @NormalizedLeaveTypeCode nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@LeaveTypeCode)),
            N''
        );

    IF @StartDate IS NULL
    BEGIN
        THROW 53401,
            N'StartDate is required.',
            1;
    END;

    IF @EndDate IS NULL
    BEGIN
        THROW 53402,
            N'EndDate is required.',
            1;
    END;

    IF @EndDate < @StartDate
    BEGIN
        THROW 53403,
            N'EndDate cannot be earlier than StartDate.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 53404,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @NormalizedLeaveTypeCode IS NULL
    BEGIN
        THROW 53405,
            N'LeaveTypeCode is required.',
            1;
    END;

    IF LEN(@NormalizedLeaveTypeCode) > 50
    BEGIN
        THROW 53406,
            N'LeaveTypeCode cannot exceed 50 characters.',
            1;
    END;

    DECLARE @TotalDays int =
        DATEDIFF(
            DAY,
            @StartDate,
            @EndDate
        ) + 1;

    DECLARE @FullWeeks int =
        @TotalDays / 7;

    DECLARE @RemainingDays int =
        @TotalDays % 7;

    DECLARE @RequestedDays decimal(9, 2);

    SELECT
        @RequestedDays =
            CONVERT(
                decimal(9, 2),
                (@FullWeeks * 5)
                + COUNT(1)
            )
    FROM
    (
        VALUES
            (0),
            (1),
            (2),
            (3),
            (4),
            (5),
            (6)
    ) AS DayOffsets([DayOffset])
    WHERE DayOffsets.[DayOffset] < @RemainingDays
        AND
        (
            (
                DATEDIFF(
                    DAY,
                    CONVERT(date, '19000101'),
                    DATEADD(DAY, DayOffsets.[DayOffset], @StartDate)
                ) % 7
            ) + 7
        ) % 7 BETWEEN 0 AND 4;

    IF @RequestedDays <= 0
    BEGIN
        THROW 53407,
            N'The leave request must include at least one business day.',
            1;
    END;

    DECLARE @ActorEmailAddress nvarchar(254);
    DECLARE @ActorRoleCode nvarchar(50);
    DECLARE @IsActorUserActive bit;
    DECLARE @IsActorRoleActive bit;
    DECLARE @ActorEmployeeId int;
    DECLARE @IsActorEmployeeActive bit;
    DECLARE @IsActorDepartmentActive bit;
    DECLARE @LeaveTypeId int;
    DECLARE @AffectsVacationBalance bit;
    DECLARE @EmployeeLeaveBalanceId int;
    DECLARE @AvailableDays decimal(9, 2);
    DECLARE @LeaveRequestId int;

    DECLARE @CreatedLeaveRequest TABLE
    (
        [LeaveRequestId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [LeaveTypeId] int NOT NULL,
        [LeaveRequestStatusCode] nvarchar(30) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [RequestedDays] decimal(9, 2) NOT NULL,
        [RespondedAtUtc] datetime2(3) NULL,
        [RespondedByUserId] int NULL,
        [CancelledAtUtc] datetime2(3) NULL,
        [CancelledByUserId] int NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [CreatedByUserId] int NOT NULL,
        [UpdatedAtUtc] datetime2(3) NULL,
        [UpdatedByUserId] int NULL,
        [RowVersion] varbinary(8) NOT NULL
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @ActorEmailAddress =
                U.[EmailAddress],
            @ActorRoleCode =
                R.[RoleCode],
            @IsActorUserActive =
                U.[IsActive],
            @IsActorRoleActive =
                R.[IsActive],
            @ActorEmployeeId =
                E.[EmployeeId],
            @IsActorEmployeeActive =
                E.[IsActive],
            @IsActorDepartmentActive =
                D.[IsActive]
        FROM [Security].[Users] AS U
            WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [Security].[Roles] AS R
            ON R.[RoleId] = U.[RoleId]
        LEFT JOIN [HumanResources].[Employees] AS E
            ON E.[UserId] = U.[UserId]
        LEFT JOIN [HumanResources].[Departments] AS D
            ON D.[DepartmentId] = E.[DepartmentId]
        WHERE U.[UserId] = @ActorUserId;

        IF @ActorEmailAddress IS NULL
        BEGIN
            THROW 53408,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 53409,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 53410,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NULL
        BEGIN
            THROW 53411,
                N'The actor user is not linked to an employee.',
                1;
        END;

        IF @IsActorEmployeeActive <> 1
        BEGIN
            THROW 53412,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @IsActorDepartmentActive <> 1
        BEGIN
            THROW 53413,
                N'The actor department is inactive.',
                1;
        END;

        SELECT
            @LeaveTypeId =
                LT.[LeaveTypeId],
            @AffectsVacationBalance =
                LT.[AffectsVacationBalance]
        FROM [LeaveManagement].[LeaveTypes] AS LT
            WITH (UPDLOCK, HOLDLOCK)
        WHERE LT.[LeaveTypeCode] = @NormalizedLeaveTypeCode
            AND LT.[IsActive] = 1;

        IF @LeaveTypeId IS NULL
        BEGIN
            THROW 53414,
                N'The leave type was not found or is inactive.',
                1;
        END;

        IF @AffectsVacationBalance <> 1
        BEGIN
            THROW 53415,
                N'The selected leave type does not affect vacation balance.',
                1;
        END;

        SELECT
            @EmployeeLeaveBalanceId =
                ELB.[EmployeeLeaveBalanceId],
            @AvailableDays =
                ELB.[AvailableDays]
        FROM [LeaveManagement].[EmployeeLeaveBalances] AS ELB
            WITH (UPDLOCK, HOLDLOCK)
        WHERE ELB.[EmployeeId] = @ActorEmployeeId
            AND ELB.[LeaveTypeId] = @LeaveTypeId;

        IF @EmployeeLeaveBalanceId IS NULL
        BEGIN
            THROW 53416,
                N'The employee leave balance was not found.',
                1;
        END;

        IF @AvailableDays < @RequestedDays
        BEGIN
            THROW 53417,
                N'The employee does not have enough available vacation days.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [LeaveManagement].[LeaveRequests] AS LR
                WITH (UPDLOCK, HOLDLOCK)
            WHERE LR.[EmployeeId] = @ActorEmployeeId
                AND LR.[LeaveRequestStatusCode] = N'Pending'
        )
        BEGIN
            THROW 53418,
                N'The employee already has a pending leave request.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [LeaveManagement].[LeaveRequests] AS LR
                WITH (UPDLOCK, HOLDLOCK)
            WHERE LR.[EmployeeId] = @ActorEmployeeId
                AND LR.[LeaveTypeId] = @LeaveTypeId
                AND LR.[LeaveRequestStatusCode] IN
                (
                    N'Pending',
                    N'Approved'
                )
                AND LR.[StartDate] <= @EndDate
                AND LR.[EndDate] >= @StartDate
        )
        BEGIN
            THROW 53419,
                N'The employee already has a leave request in the selected date range.',
                1;
        END;

        UPDATE [LeaveManagement].[EmployeeLeaveBalances]
        SET
            [PendingDays] =
                [PendingDays] + @RequestedDays,
            [UpdatedAtUtc] =
                @OccurredAtUtc,
            [UpdatedByUserId] =
                @ActorUserId
        WHERE [EmployeeLeaveBalanceId] = @EmployeeLeaveBalanceId;

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 53420,
                N'The leave balance update returned an unexpected row count.',
                1;
        END;

        INSERT INTO [LeaveManagement].[LeaveRequests]
        (
            [EmployeeId],
            [LeaveTypeId],
            [StartDate],
            [EndDate],
            [RequestedDays],
            [CreatedByUserId]
        )
        OUTPUT
            INSERTED.[LeaveRequestId],
            INSERTED.[EmployeeId],
            INSERTED.[LeaveTypeId],
            INSERTED.[LeaveRequestStatusCode],
            INSERTED.[StartDate],
            INSERTED.[EndDate],
            INSERTED.[RequestedDays],
            INSERTED.[RespondedAtUtc],
            INSERTED.[RespondedByUserId],
            INSERTED.[CancelledAtUtc],
            INSERTED.[CancelledByUserId],
            INSERTED.[CreatedAtUtc],
            INSERTED.[CreatedByUserId],
            INSERTED.[UpdatedAtUtc],
            INSERTED.[UpdatedByUserId],
            INSERTED.[RowVersion]
        INTO @CreatedLeaveRequest
        (
            [LeaveRequestId],
            [EmployeeId],
            [LeaveTypeId],
            [LeaveRequestStatusCode],
            [StartDate],
            [EndDate],
            [RequestedDays],
            [RespondedAtUtc],
            [RespondedByUserId],
            [CancelledAtUtc],
            [CancelledByUserId],
            [CreatedAtUtc],
            [CreatedByUserId],
            [UpdatedAtUtc],
            [UpdatedByUserId],
            [RowVersion]
        )
        VALUES
        (
            @ActorEmployeeId,
            @LeaveTypeId,
            @StartDate,
            @EndDate,
            @RequestedDays,
            @ActorUserId
        );

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 53421,
                N'The leave request insert returned an unexpected row count.',
                1;
        END;

        SELECT
            @LeaveRequestId =
                CLR.[LeaveRequestId]
        FROM @CreatedLeaveRequest AS CLR;

        INSERT INTO [LeaveManagement].[LeaveRequestStatusHistory]
        (
            [LeaveRequestId],
            [FromLeaveRequestStatusCode],
            [ToLeaveRequestStatusCode],
            [ChangedAtUtc],
            [ChangedByUserId]
        )
        VALUES
        (
            @LeaveRequestId,
            NULL,
            N'Pending',
            @OccurredAtUtc,
            @ActorUserId
        );

        INSERT INTO [LeaveManagement].[LeaveBalanceTransactions]
        (
            [EmployeeLeaveBalanceId],
            [LeaveRequestId],
            [TransactionTypeCode],
            [PendingDaysDelta],
            [CreatedByUserId]
        )
        VALUES
        (
            @EmployeeLeaveBalanceId,
            @LeaveRequestId,
            N'PendingReservation',
            @RequestedDays,
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
            [NewValuesJson],
            [OccurredAtUtc]
        )
        VALUES
        (
            @ResolvedCorrelationId,
            N'LeaveManagement',
            N'LeaveRequestCreated',
            N'LeaveRequests',
            CONVERT(
                nvarchar(100),
                @LeaveRequestId
            ),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Leave request created successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            (
                SELECT
                    CLR.[LeaveRequestId],
                    CLR.[EmployeeId],
                    CLR.[LeaveTypeId],
                    CLR.[LeaveRequestStatusCode],
                    CLR.[StartDate],
                    CLR.[EndDate],
                    CLR.[RequestedDays]
                FROM @CreatedLeaveRequest AS CLR
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            LR.[LeaveRequestId],
            LR.[EmployeeId],
            E.[IdentificationNumber],
            E.[FirstName],
            E.[LastName],
            D.[DepartmentId],
            D.[DepartmentCode],
            D.[Name] AS [DepartmentName],
            LR.[LeaveTypeId],
            LT.[LeaveTypeCode],
            LT.[Name] AS [LeaveTypeName],
            LR.[LeaveRequestStatusCode],
            LRS.[Name] AS [LeaveRequestStatusName],
            LR.[StartDate],
            LR.[EndDate],
            LR.[RequestedDays],
            LR.[RespondedAtUtc],
            LR.[RespondedByUserId],
            LR.[CancelledAtUtc],
            LR.[CancelledByUserId],
            LR.[CreatedAtUtc],
            LR.[CreatedByUserId],
            LR.[UpdatedAtUtc],
            LR.[UpdatedByUserId],
            LR.[RowVersion]
        FROM @CreatedLeaveRequest AS LR
        INNER JOIN [HumanResources].[Employees] AS E
            ON E.[EmployeeId] = LR.[EmployeeId]
        INNER JOIN [HumanResources].[Departments] AS D
            ON D.[DepartmentId] = E.[DepartmentId]
        INNER JOIN [LeaveManagement].[LeaveTypes] AS LT
            ON LT.[LeaveTypeId] = LR.[LeaveTypeId]
        INNER JOIN [LeaveManagement].[LeaveRequestStatuses] AS LRS
            ON LRS.[LeaveRequestStatusCode] = LR.[LeaveRequestStatusCode];
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
