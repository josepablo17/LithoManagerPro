CREATE PROCEDURE [LeaveManagement].[AdjustEmployeeLeaveBalance]
    @EmployeeId int,
    @LeaveTypeCode nvarchar(4000) = N'Vacation',
    @AdjustedDaysDelta decimal(9, 2),
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

    DECLARE @NormalizedLeaveTypeCode nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@LeaveTypeCode)),
            N''
        );

    IF @EmployeeId IS NULL
       OR @EmployeeId <= 0
    BEGIN
        THROW 53301,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @NormalizedLeaveTypeCode IS NULL
    BEGIN
        THROW 53302,
            N'LeaveTypeCode is required.',
            1;
    END;

    IF LEN(@NormalizedLeaveTypeCode) > 50
    BEGIN
        THROW 53303,
            N'LeaveTypeCode cannot exceed 50 characters.',
            1;
    END;

    IF @AdjustedDaysDelta IS NULL
       OR @AdjustedDaysDelta = 0
    BEGIN
        THROW 53304,
            N'AdjustedDaysDelta must be different from zero.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 53305,
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

    DECLARE @LeaveTypeId int;
    DECLARE @LeavePolicyId int;
    DECLARE @TargetEmployeeName nvarchar(251);
    DECLARE @IsTargetEmployeeActive bit;
    DECLARE @IsTargetDepartmentActive bit;
    DECLARE @EmployeeLeaveBalanceId int;
    DECLARE @PreviousValuesJson nvarchar(max);

    DECLARE @ResultBalance TABLE
    (
        [EmployeeLeaveBalanceId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [LeaveTypeId] int NOT NULL,
        [LeavePolicyId] int NOT NULL,
        [AccruedDays] decimal(9, 2) NOT NULL,
        [AdjustedDays] decimal(9, 2) NOT NULL,
        [PendingDays] decimal(9, 2) NOT NULL,
        [UsedDays] decimal(9, 2) NOT NULL,
        [AvailableDays] decimal(9, 2) NOT NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL,
        [CreatedByUserId] int NULL,
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
            THROW 53306,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 53307,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 53308,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 53309,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 53310,
                N'The actor department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator'
        )
        BEGIN
            THROW 53311,
                N'The actor role is not allowed to adjust leave balances.',
                1;
        END;

        SELECT
            @TargetEmployeeName =
                E.[FirstName] + N' ' + E.[LastName],
            @IsTargetEmployeeActive =
                E.[IsActive],
            @IsTargetDepartmentActive =
                D.[IsActive]
        FROM [HumanResources].[Employees] AS E
            WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [HumanResources].[Departments] AS D
            ON D.[DepartmentId] = E.[DepartmentId]
        WHERE E.[EmployeeId] = @EmployeeId;

        IF @TargetEmployeeName IS NULL
        BEGIN
            THROW 53312,
                N'The employee was not found.',
                1;
        END;

        IF @IsTargetEmployeeActive <> 1
        BEGIN
            THROW 53313,
                N'The employee record is inactive.',
                1;
        END;

        IF @IsTargetDepartmentActive <> 1
        BEGIN
            THROW 53314,
                N'The employee department is inactive.',
                1;
        END;

        SELECT
            @LeaveTypeId =
                LT.[LeaveTypeId]
        FROM [LeaveManagement].[LeaveTypes] AS LT
            WITH (UPDLOCK, HOLDLOCK)
        WHERE LT.[LeaveTypeCode] = @NormalizedLeaveTypeCode
            AND LT.[IsActive] = 1;

        IF @LeaveTypeId IS NULL
        BEGIN
            THROW 53315,
                N'The leave type was not found or is inactive.',
                1;
        END;

        SELECT
            @LeavePolicyId =
                LP.[LeavePolicyId]
        FROM [LeaveManagement].[LeavePolicies] AS LP
            WITH (UPDLOCK, HOLDLOCK)
        WHERE LP.[LeaveTypeId] = @LeaveTypeId
            AND LP.[IsActive] = 1;

        IF @LeavePolicyId IS NULL
        BEGIN
            THROW 53316,
                N'The active leave policy was not found.',
                1;
        END;

        SELECT
            @EmployeeLeaveBalanceId =
                ELB.[EmployeeLeaveBalanceId],
            @PreviousValuesJson =
            (
                SELECT
                    ELB.[EmployeeLeaveBalanceId],
                    ELB.[EmployeeId],
                    ELB.[LeaveTypeId],
                    ELB.[LeavePolicyId],
                    ELB.[AccruedDays],
                    ELB.[AdjustedDays],
                    ELB.[PendingDays],
                    ELB.[UsedDays],
                    ELB.[AvailableDays]
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            )
        FROM [LeaveManagement].[EmployeeLeaveBalances] AS ELB
            WITH (UPDLOCK, HOLDLOCK)
        WHERE ELB.[EmployeeId] = @EmployeeId
            AND ELB.[LeaveTypeId] = @LeaveTypeId;

        IF @EmployeeLeaveBalanceId IS NULL
        BEGIN
            INSERT INTO [LeaveManagement].[EmployeeLeaveBalances]
            (
                [EmployeeId],
                [LeaveTypeId],
                [LeavePolicyId],
                [CreatedByUserId]
            )
            VALUES
            (
                @EmployeeId,
                @LeaveTypeId,
                @LeavePolicyId,
                @ActorUserId
            );

            SET @EmployeeLeaveBalanceId =
                SCOPE_IDENTITY();
        END;

        UPDATE [LeaveManagement].[EmployeeLeaveBalances]
        SET
            [AdjustedDays] =
                [AdjustedDays] + @AdjustedDaysDelta,
            [UpdatedAtUtc] =
                @OccurredAtUtc,
            [UpdatedByUserId] =
                @ActorUserId
        OUTPUT
            INSERTED.[EmployeeLeaveBalanceId],
            INSERTED.[EmployeeId],
            INSERTED.[LeaveTypeId],
            INSERTED.[LeavePolicyId],
            INSERTED.[AccruedDays],
            INSERTED.[AdjustedDays],
            INSERTED.[PendingDays],
            INSERTED.[UsedDays],
            INSERTED.[AvailableDays],
            INSERTED.[CreatedAtUtc],
            INSERTED.[CreatedByUserId],
            INSERTED.[UpdatedAtUtc],
            INSERTED.[UpdatedByUserId],
            INSERTED.[RowVersion]
        INTO @ResultBalance
        (
            [EmployeeLeaveBalanceId],
            [EmployeeId],
            [LeaveTypeId],
            [LeavePolicyId],
            [AccruedDays],
            [AdjustedDays],
            [PendingDays],
            [UsedDays],
            [AvailableDays],
            [CreatedAtUtc],
            [CreatedByUserId],
            [UpdatedAtUtc],
            [UpdatedByUserId],
            [RowVersion]
        )
        WHERE [EmployeeLeaveBalanceId] = @EmployeeLeaveBalanceId;

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 53317,
                N'The leave balance adjustment returned an unexpected row count.',
                1;
        END;

        INSERT INTO [LeaveManagement].[LeaveBalanceTransactions]
        (
            [EmployeeLeaveBalanceId],
            [TransactionTypeCode],
            [AdjustedDaysDelta],
            [CreatedByUserId]
        )
        VALUES
        (
            @EmployeeLeaveBalanceId,
            N'Adjustment',
            @AdjustedDaysDelta,
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
            N'LeaveBalanceAdjusted',
            N'EmployeeLeaveBalances',
            CONVERT(
                nvarchar(100),
                @EmployeeLeaveBalanceId
            ),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Employee leave balance adjusted successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'PATCH',
            @RequestPath,
            @PreviousValuesJson,
            (
                SELECT
                    RB.[EmployeeLeaveBalanceId],
                    RB.[EmployeeId],
                    RB.[LeaveTypeId],
                    RB.[LeavePolicyId],
                    RB.[AccruedDays],
                    RB.[AdjustedDays],
                    RB.[PendingDays],
                    RB.[UsedDays],
                    RB.[AvailableDays]
                FROM @ResultBalance AS RB
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            RB.[EmployeeLeaveBalanceId],
            RB.[EmployeeId],
            @TargetEmployeeName AS [EmployeeName],
            RB.[LeaveTypeId],
            @NormalizedLeaveTypeCode AS [LeaveTypeCode],
            RB.[LeavePolicyId],
            RB.[AccruedDays],
            RB.[AdjustedDays],
            RB.[PendingDays],
            RB.[UsedDays],
            RB.[AvailableDays],
            RB.[CreatedAtUtc],
            RB.[CreatedByUserId],
            RB.[UpdatedAtUtc],
            RB.[UpdatedByUserId],
            RB.[RowVersion]
        FROM @ResultBalance AS RB;
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
