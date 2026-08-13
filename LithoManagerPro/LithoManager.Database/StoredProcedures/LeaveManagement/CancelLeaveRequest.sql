CREATE PROCEDURE [LeaveManagement].[CancelLeaveRequest]
    @LeaveRequestId int,
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
        COALESCE(
            @CorrelationId,
            NEWID()
        );

    IF @LeaveRequestId IS NULL
       OR @LeaveRequestId <= 0
    BEGIN
        THROW 53501,
            N'LeaveRequestId must be greater than zero.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
       OR DATALENGTH(@ExpectedRowVersion) <> 8
    BEGIN
        THROW 53502,
            N'ExpectedRowVersion must contain exactly 8 bytes.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 53503,
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

    DECLARE @RequestEmployeeId int;
    DECLARE @LeaveTypeId int;
    DECLARE @CurrentStatusCode nvarchar(30);
    DECLARE @RequestedDays decimal(9, 2);
    DECLARE @ExistingRowVersion varbinary(8);
    DECLARE @EmployeeLeaveBalanceId int;
    DECLARE @PreviousValuesJson nvarchar(max);

    DECLARE @ResultLeaveRequest TABLE
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
            THROW 53504,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 53505,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 53506,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 53507,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 53508,
                N'The actor department is inactive.',
                1;
        END;

        SELECT
            @RequestEmployeeId =
                LR.[EmployeeId],
            @LeaveTypeId =
                LR.[LeaveTypeId],
            @CurrentStatusCode =
                LR.[LeaveRequestStatusCode],
            @RequestedDays =
                LR.[RequestedDays],
            @ExistingRowVersion =
                LR.[RowVersion],
            @PreviousValuesJson =
            (
                SELECT
                    LR.[LeaveRequestId],
                    LR.[EmployeeId],
                    LR.[LeaveTypeId],
                    LR.[LeaveRequestStatusCode],
                    LR.[StartDate],
                    LR.[EndDate],
                    LR.[RequestedDays]
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            )
        FROM [LeaveManagement].[LeaveRequests] AS LR
            WITH (UPDLOCK, HOLDLOCK)
        WHERE LR.[LeaveRequestId] = @LeaveRequestId;

        IF @RequestEmployeeId IS NULL
        BEGIN
            THROW 53509,
                N'The leave request was not found.',
                1;
        END;

        IF @ExistingRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 53510,
                N'The leave request has been modified by another transaction.',
                1;
        END;

        IF @CurrentStatusCode <> N'Pending'
        BEGIN
            THROW 53511,
                N'Only pending leave requests can be cancelled.',
                1;
        END;

        IF @RequestEmployeeId <> @ActorEmployeeId
           AND @ActorRoleCode NOT IN
           (
               N'SuperAdministrator',
               N'HumanResourcesAdministrator'
           )
        BEGIN
            THROW 53512,
                N'The actor is not allowed to cancel this leave request.',
                1;
        END;

        SELECT
            @EmployeeLeaveBalanceId =
                ELB.[EmployeeLeaveBalanceId]
        FROM [LeaveManagement].[EmployeeLeaveBalances] AS ELB
            WITH (UPDLOCK, HOLDLOCK)
        WHERE ELB.[EmployeeId] = @RequestEmployeeId
            AND ELB.[LeaveTypeId] = @LeaveTypeId;

        IF @EmployeeLeaveBalanceId IS NULL
        BEGIN
            THROW 53513,
                N'The employee leave balance was not found.',
                1;
        END;

        UPDATE [LeaveManagement].[LeaveRequests]
        SET
            [LeaveRequestStatusCode] =
                N'Cancelled',
            [CancelledAtUtc] =
                @OccurredAtUtc,
            [CancelledByUserId] =
                @ActorUserId,
            [UpdatedAtUtc] =
                @OccurredAtUtc,
            [UpdatedByUserId] =
                @ActorUserId
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
        INTO @ResultLeaveRequest
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
        WHERE [LeaveRequestId] = @LeaveRequestId;

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 53514,
                N'The leave request cancellation returned an unexpected row count.',
                1;
        END;

        UPDATE [LeaveManagement].[EmployeeLeaveBalances]
        SET
            [PendingDays] =
                [PendingDays] - @RequestedDays,
            [UpdatedAtUtc] =
                @OccurredAtUtc,
            [UpdatedByUserId] =
                @ActorUserId
        WHERE [EmployeeLeaveBalanceId] = @EmployeeLeaveBalanceId;

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 53515,
                N'The leave balance update returned an unexpected row count.',
                1;
        END;

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
            N'Pending',
            N'Cancelled',
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
            N'PendingRelease',
            -@RequestedDays,
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
        VALUES
        (
            @ResolvedCorrelationId,
            N'LeaveManagement',
            N'LeaveRequestCancelled',
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
            N'Leave request cancelled successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'PATCH',
            @RequestPath,
            @PreviousValuesJson,
            (
                SELECT
                    RLR.[LeaveRequestId],
                    RLR.[EmployeeId],
                    RLR.[LeaveTypeId],
                    RLR.[LeaveRequestStatusCode],
                    RLR.[StartDate],
                    RLR.[EndDate],
                    RLR.[RequestedDays],
                    RLR.[CancelledAtUtc],
                    RLR.[CancelledByUserId]
                FROM @ResultLeaveRequest AS RLR
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
        FROM @ResultLeaveRequest AS LR
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
