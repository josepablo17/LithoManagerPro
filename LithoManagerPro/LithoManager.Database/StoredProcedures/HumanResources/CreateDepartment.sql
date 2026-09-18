CREATE PROCEDURE [HumanResources].[CreateDepartment]
    @DepartmentCode nvarchar(4000),
    @Name nvarchar(4000),
    @Description nvarchar(4000) = NULL,
    @ActorUserId int,
    @CorrelationId uniqueidentifier = NULL,
    @ClientIpAddress nvarchar(45) = NULL,
    @UserAgent nvarchar(512) = NULL,
    @RequestPath nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /*
        General values
    */

    DECLARE @OccurredAtUtc datetime2(3) =
        SYSUTCDATETIME();

    DECLARE @ResolvedCorrelationId uniqueidentifier =
        COALESCE(
            @CorrelationId,
            NEWID()
        );

    /*
        Normalize input values
    */

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

    /*
        Basic input validation
    */

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 52001,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @NormalizedDepartmentCode IS NULL
    BEGIN
        THROW 52002,
            N'DepartmentCode is required.',
            1;
    END;

    IF LEN(@NormalizedDepartmentCode) > 50
    BEGIN
        THROW 52003,
            N'DepartmentCode cannot exceed 50 characters.',
            1;
    END;

    IF @NormalizedDepartmentCode LIKE N'% %'
    BEGIN
        THROW 52004,
            N'DepartmentCode cannot contain spaces.',
            1;
    END;

    IF @NormalizedName IS NULL
    BEGIN
        THROW 52005,
            N'Name is required.',
            1;
    END;

    IF LEN(@NormalizedName) > 100
    BEGIN
        THROW 52006,
            N'Name cannot exceed 100 characters.',
            1;
    END;

    IF @NormalizedDescription IS NOT NULL
       AND LEN(@NormalizedDescription) > 300
    BEGIN
        THROW 52007,
            N'Description cannot exceed 300 characters.',
            1;
    END;

    /*
        Actor information
    */

    DECLARE @ActorEmailAddress nvarchar(254);
    DECLARE @ActorRoleCode nvarchar(50);

    DECLARE @IsActorUserActive bit;
    DECLARE @IsActorRoleActive bit;

    DECLARE @ActorEmployeeId int;
    DECLARE @IsActorEmployeeActive bit;
    DECLARE @IsActorDepartmentActive bit;

    /*
        Created department information
    */

    DECLARE @DepartmentId int;
    DECLARE @NewValuesJson nvarchar(max);

    DECLARE @CreatedDepartment TABLE
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

        /*
            Resolve authenticated actor.

            Authorization can also exist at API level,
            but the database requires valid actor data
            for authorization and audit logging.
        */

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

        /*
            Validate actor
        */

        IF @ActorEmailAddress IS NULL
        BEGIN
            THROW 52008,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive <> 1
        BEGIN
            THROW 52009,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive <> 1
        BEGIN
            THROW 52010,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 52011,
                N'The actor employee record is inactive.',
                1;
        END;

        /*
            Only validate the department when the employee
            is actually assigned to one.

            If DepartmentId is optional for an employee,
            a NULL department should not invalidate the actor.
        */

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive = 0
        BEGIN
            THROW 52012,
                N'The actor department is inactive.',
                1;
        END;

        /*
            Database-level authorization
        */

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator'
        )
        BEGIN
            THROW 52013,
                N'The actor role is not allowed to create departments.',
                1;
        END;

        /*
            Validate DepartmentCode uniqueness
        */

        IF EXISTS
        (
            SELECT 1
            FROM [HumanResources].[Departments] AS D
                WITH (UPDLOCK, HOLDLOCK)
            WHERE D.[DepartmentCode] =
                @NormalizedDepartmentCode
        )
        BEGIN
            THROW 52014,
                N'A department with the same DepartmentCode already exists.',
                1;
        END;

        /*
            Validate Name uniqueness
        */

        IF EXISTS
        (
            SELECT 1
            FROM [HumanResources].[Departments] AS D
                WITH (UPDLOCK, HOLDLOCK)
            WHERE D.[Name] =
                @NormalizedName
        )
        BEGIN
            THROW 52015,
                N'A department with the same Name already exists.',
                1;
        END;

        /*
            Create department
        */

        INSERT INTO [HumanResources].[Departments]
        (
            [DepartmentCode],
            [Name],
            [Description],
            [IsActive],
            [CreatedByUserId]
        )
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
        INTO @CreatedDepartment
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
        VALUES
        (
            @NormalizedDepartmentCode,
            @NormalizedName,
            @NormalizedDescription,
            1,
            @ActorUserId
        );

        /*
            A Create operation must insert exactly one row.
        */

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 52016,
                N'The department insert returned an unexpected row count.',
                1;
        END;

        /*
            Resolve generated DepartmentId
        */

        SELECT
            @DepartmentId =
                D.[DepartmentId]
        FROM @CreatedDepartment AS D;

        IF @DepartmentId IS NULL
        BEGIN
            THROW 52017,
                N'The generated DepartmentId could not be resolved.',
                1;
        END;

        /*
            Build audit JSON
        */

        SET @NewValuesJson =
        (
            SELECT
                D.[DepartmentId],
                D.[DepartmentCode],
                D.[Name],
                D.[Description],
                D.[IsActive]
            FROM @CreatedDepartment AS D
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        /*
            Audit successful operation
        */

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
            N'HumanResources',
            N'DepartmentCreated',
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
            N'Department created successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            @NewValuesJson,
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        /*
            Return created department.
        */

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
        FROM @CreatedDepartment AS D;

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