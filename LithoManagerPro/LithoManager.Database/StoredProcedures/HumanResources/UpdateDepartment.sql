CREATE PROCEDURE [HumanResources].[UpdateDepartment]
    @DepartmentId int,
    @DepartmentCode nvarchar(4000),
    @Name nvarchar(4000),
    @Description nvarchar(4000) = NULL,
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

    DECLARE @NormalizedDepartmentCode nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@DepartmentCode)),
            N''
        );

    DECLARE @NormalizedName nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@Name)),
            N''
        );

    DECLARE @NormalizedDescription nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@Description)),
            N''
        );

    IF @DepartmentId IS NULL
       OR @DepartmentId <= 0
    BEGIN
        THROW 52031,
            N'The DepartmentId must be greater than zero.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 52032,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
       OR DATALENGTH(@ExpectedRowVersion) <> 8
    BEGIN
        THROW 52033,
            N'ExpectedRowVersion must contain exactly 8 bytes.',
            1;
    END;

    IF @NormalizedDepartmentCode IS NULL
    BEGIN
        THROW 52034,
            N'DepartmentCode is required.',
            1;
    END;

    IF LEN(@NormalizedDepartmentCode) > 50
    BEGIN
        THROW 52035,
            N'DepartmentCode cannot exceed 50 characters.',
            1;
    END;

    IF @NormalizedDepartmentCode LIKE N'% %'
    BEGIN
        THROW 52036,
            N'DepartmentCode cannot contain spaces.',
            1;
    END;

    IF @NormalizedName IS NULL
    BEGIN
        THROW 52037,
            N'Name is required.',
            1;
    END;

    IF LEN(@NormalizedName) > 100
    BEGIN
        THROW 52038,
            N'Name cannot exceed 100 characters.',
            1;
    END;

    IF @NormalizedDescription IS NOT NULL
       AND LEN(@NormalizedDescription) > 300
    BEGIN
        THROW 52039,
            N'Description cannot exceed 300 characters.',
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

    DECLARE @UpdatedDepartment TABLE
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
            THROW 52040,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 52041,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 52042,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 52043,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 52044,
                N'The actor department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator'
        )
        BEGIN
            THROW 52045,
                N'The actor role is not allowed to update departments.',
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
            THROW 52046,
                N'The department was not found.',
                1;
        END;

        IF @ExistingRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 52047,
                N'The department has been modified by another transaction.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [HumanResources].[Departments] AS D
                WITH (UPDLOCK, HOLDLOCK)
            WHERE D.[DepartmentCode] =
                @NormalizedDepartmentCode
              AND D.[DepartmentId] <>
                @DepartmentId
        )
        BEGIN
            THROW 52048,
                N'A department with the same DepartmentCode already exists.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [HumanResources].[Departments] AS D
                WITH (UPDLOCK, HOLDLOCK)
            WHERE D.[Name] = @NormalizedName
              AND D.[DepartmentId] <>
                @DepartmentId
        )
        BEGIN
            THROW 52049,
                N'A department with the same Name already exists.',
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

        UPDATE [HumanResources].[Departments]
        SET
            [DepartmentCode] =
                @NormalizedDepartmentCode,

            [Name] =
                @NormalizedName,

            [Description] =
                @NormalizedDescription,

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
        INTO @UpdatedDepartment
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
            THROW 52050,
                N'The department update returned an unexpected row count.',
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
        VALUES
        (
            @ResolvedCorrelationId,
            N'HumanResources',
            N'DepartmentUpdated',
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
            N'Department updated successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'PUT',
            @RequestPath,
            @PreviousValuesJson,
            (
                SELECT
                    D.[DepartmentId],
                    D.[DepartmentCode],
                    D.[Name],
                    D.[Description],
                    D.[IsActive]
                FROM @UpdatedDepartment AS D
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
        FROM @UpdatedDepartment AS D;
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
