CREATE PROCEDURE [LeaveManagement].[RespondLeaveRequest]
    @LeaveRequestId int,
    @IsApproved bit,
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
        THROW 53601,
            N'LeaveRequestId must be greater than zero.',
            1;
    END;

    IF @IsApproved IS NULL
    BEGIN
        THROW 53602,
            N'IsApproved is required.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
       OR DATALENGTH(@ExpectedRowVersion) <> 8
    BEGIN
        THROW 53603,
            N'ExpectedRowVersion must contain exactly 8 bytes.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 53604,
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
    DECLARE @NewStatusCode nvarchar(30);
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

    SET @NewStatusCode =
        CASE
            WHEN @IsApproved = 1
                THEN N'Approved'
            ELSE N'Rejected'
        END;

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
            THROW 53605,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 53606,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 53607,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 53608,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 53609,
                N'The actor department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator'
        )
        BEGIN
            THROW 53610,
                N'The actor role is not allowed to respond leave requests.',
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
            THROW 53611,
                N'The leave request was not found.',
                1;
        END;

        IF @ExistingRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 53612,
                N'The leave request has been modified by another transaction.',
                1;
        END;

        IF @CurrentStatusCode <> N'Pending'
        BEGIN
            THROW 53613,
                N'Only pending leave requests can be responded.',
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
            THROW 53614,
                N'The employee leave balance was not found.',
                1;
        END;

        UPDATE [LeaveManagement].[LeaveRequests]
        SET
            [LeaveRequestStatusCode] =
                @NewStatusCode,
            [RespondedAtUtc] =
                @OccurredAtUtc,
            [RespondedByUserId] =
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
            THROW 53615,
                N'The leave request response returned an unexpected row count.',
                1;
        END;

        UPDATE [LeaveManagement].[EmployeeLeaveBalances]
        SET
            [PendingDays] =
                [PendingDays] - @RequestedDays,
            [UsedDays] =
                CASE
                    WHEN @IsApproved = 1
                        THEN [UsedDays] + @RequestedDays
                    ELSE [UsedDays]
                END,
            [UpdatedAtUtc] =
                @OccurredAtUtc,
            [UpdatedByUserId] =
                @ActorUserId
        WHERE [EmployeeLeaveBalanceId] = @EmployeeLeaveBalanceId;

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 53616,
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
            @NewStatusCode,
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

        IF @IsApproved = 1
        BEGIN
            INSERT INTO [LeaveManagement].[LeaveBalanceTransactions]
            (
                [EmployeeLeaveBalanceId],
                [LeaveRequestId],
                [TransactionTypeCode],
                [UsedDaysDelta],
                [CreatedByUserId]
            )
            VALUES
            (
                @EmployeeLeaveBalanceId,
                @LeaveRequestId,
                N'Usage',
                @RequestedDays,
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
        VALUES
        (
            @ResolvedCorrelationId,
            N'LeaveManagement',
            CASE
                WHEN @IsApproved = 1
                    THEN N'LeaveRequestApproved'
                ELSE N'LeaveRequestRejected'
            END,
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
            CASE
                WHEN @IsApproved = 1
                    THEN N'Leave request approved successfully.'
                ELSE N'Leave request rejected successfully.'
            END,
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
                    RLR.[RespondedAtUtc],
                    RLR.[RespondedByUserId]
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
