CREATE PROCEDURE [HumanResources].[SetDepartmentStatus]
    @DepartmentId int,
    @IsActive bit,
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

    IF @DepartmentId IS NULL
       OR @DepartmentId <= 0
    BEGIN
        THROW 52061,
            N'The DepartmentId must be greater than zero.',
            1;
    END;

    IF @IsActive IS NULL
    BEGIN
        THROW 52062,
            N'IsActive is required.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
       OR DATALENGTH(@ExpectedRowVersion) <> 8
    BEGIN
        THROW 52063,
            N'ExpectedRowVersion must contain exactly 8 bytes.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 52064,
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

    DECLARE @ExistingDepartmentCode nvarchar(50);
    DECLARE @ExistingName nvarchar(100);
    DECLARE @ExistingDescription nvarchar(300);
    DECLARE @ExistingIsActive bit;
    DECLARE @ExistingRowVersion varbinary(8);
    DECLARE @PreviousValuesJson nvarchar(max);

    DECLARE @ResultDepartment TABLE
    (
        [DepartmentId] int NOT NULL,
        [DepartmentCode] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(300) NULL,
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
            THROW 52065,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 52066,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 52067,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 52068,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 52069,
                N'The actor department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator'
        )
        BEGIN
            THROW 52070,
                N'The actor role is not allowed to set department status.',
                1;
        END;

        SELECT
            @ExistingDepartmentCode =
                D.[DepartmentCode],

            @ExistingName =
                D.[Name],

            @ExistingDescription =
                D.[Description],

            @ExistingIsActive =
                D.[IsActive],

            @ExistingRowVersion =
                D.[RowVersion]

        FROM [HumanResources].[Departments] AS D
            WITH (UPDLOCK, HOLDLOCK)
        WHERE D.[DepartmentId] = @DepartmentId;

        IF @ExistingDepartmentCode IS NULL
        BEGIN
            THROW 52071,
                N'The department was not found.',
                1;
        END;

        IF @ExistingRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 52072,
                N'The department has been modified by another transaction.',
                1;
        END;

        IF @IsActive = 0
           AND EXISTS
           (
               SELECT 1
               FROM [HumanResources].[Employees] AS E
                   WITH (UPDLOCK, HOLDLOCK)
               WHERE E.[DepartmentId] =
                   @DepartmentId
                 AND E.[IsActive] = 1
           )
        BEGIN
            THROW 52074,
                N'The department cannot be deactivated while it has active employees.',
                1;
        END;

        SET @PreviousValuesJson =
        (
            SELECT
                @DepartmentId AS [DepartmentId],
                @ExistingDepartmentCode AS [DepartmentCode],
                @ExistingName AS [Name],
                @ExistingDescription AS [Description],
                @ExistingIsActive AS [IsActive]
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        IF @ExistingIsActive = @IsActive
        BEGIN
            INSERT INTO @ResultDepartment
            (
                [DepartmentId],
                [DepartmentCode],
                [Name],
                [Description],
                [IsActive],
                [CreatedAtUtc],
                [CreatedByUserId],
                [UpdatedAtUtc],
                [UpdatedByUserId],
                [RowVersion]
            )
            SELECT
                D.[DepartmentId],
                D.[DepartmentCode],
                D.[Name],
                D.[Description],
                D.[IsActive],
                D.[CreatedAtUtc],
                D.[CreatedByUserId],
                D.[UpdatedAtUtc],
                D.[UpdatedByUserId],
                D.[RowVersion]
            FROM [HumanResources].[Departments] AS D
            WHERE D.[DepartmentId] = @DepartmentId;
        END;
        ELSE
        BEGIN
            UPDATE [HumanResources].[Departments]
            SET
                [IsActive] =
                    @IsActive,

                [UpdatedAtUtc] =
                    @OccurredAtUtc,

                [UpdatedByUserId] =
                    @ActorUserId
            OUTPUT
                INSERTED.[DepartmentId],
                INSERTED.[DepartmentCode],
                INSERTED.[Name],
                INSERTED.[Description],
                INSERTED.[IsActive],
                INSERTED.[CreatedAtUtc],
                INSERTED.[CreatedByUserId],
                INSERTED.[UpdatedAtUtc],
                INSERTED.[UpdatedByUserId],
                INSERTED.[RowVersion]
            INTO @ResultDepartment
            (
                [DepartmentId],
                [DepartmentCode],
                [Name],
                [Description],
                [IsActive],
                [CreatedAtUtc],
                [CreatedByUserId],
                [UpdatedAtUtc],
                [UpdatedByUserId],
                [RowVersion]
            )
            WHERE [DepartmentId] = @DepartmentId;

            IF @@ROWCOUNT <> 1
            BEGIN
                THROW 52073,
                    N'The department status update returned an unexpected row count.',
                    1;
            END;
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
            N'HumanResources',
            N'DepartmentStatusSet',
            N'Departments',
            CONVERT(
                nvarchar(100),
                @DepartmentId
            ),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            CASE
                WHEN @ExistingIsActive = @IsActive
                    THEN N'Department status was already set.'
                ELSE N'Department status updated successfully.'
            END,
            @ClientIpAddress,
            @UserAgent,
            N'PATCH',
            @RequestPath,
            @PreviousValuesJson,
            (
                SELECT
                    D.[DepartmentId],
                    D.[DepartmentCode],
                    D.[Name],
                    D.[Description],
                    D.[IsActive]
                FROM @ResultDepartment AS D
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            D.[DepartmentId],
            D.[DepartmentCode],
            D.[Name],
            D.[Description],
            D.[IsActive],
            D.[CreatedAtUtc],
            D.[CreatedByUserId],
            D.[UpdatedAtUtc],
            D.[UpdatedByUserId],
            D.[RowVersion]
        FROM @ResultDepartment AS D;
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
