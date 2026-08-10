CREATE PROCEDURE [HumanResources].[CreateDepartment]
    @DepartmentCode nvarchar(50),
    @Name nvarchar(100),
    @Description nvarchar(300) = NULL,
    @CreatedByUserId int,
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

    DECLARE @NormalizedDepartmentCode nvarchar(50) =
        LTRIM(RTRIM(@DepartmentCode));

    DECLARE @NormalizedName nvarchar(100) =
        LTRIM(RTRIM(@Name));

    DECLARE @NormalizedDescription nvarchar(300) =
        NULLIF(
            LTRIM(RTRIM(@Description)),
            N''
        );

    /*
        Basic validations
    */

    IF NULLIF(
        @NormalizedDepartmentCode,
        N'') IS NULL
    BEGIN
        THROW 52101,
            N'The department code is required.',
            1;
    END;

    IF NULLIF(
        @NormalizedName,
        N'') IS NULL
    BEGIN
        THROW 52102,
            N'The department name is required.',
            1;
    END;

    IF @NormalizedDepartmentCode LIKE N'% %'
    BEGIN
        THROW 52103,
            N'The department code cannot contain spaces.',
            1;
    END;

    IF @CreatedByUserId <= 0
    BEGIN
        THROW 52104,
            N'The creator user identifier is invalid.',
            1;
    END;

    DECLARE @ActorEmailAddress nvarchar(254);
    DECLARE @ActorRoleCode nvarchar(50);
    DECLARE @DepartmentId int;
    DECLARE @NewValuesJson nvarchar(max);

    BEGIN TRY
        BEGIN TRANSACTION;

        /*
            Resolve the authenticated actor.

            Authorization will also exist at API level,
            but the database needs valid actor data for
            the audit log.
        */

        SELECT
            @ActorEmailAddress =
                U.[EmailAddress],

            @ActorRoleCode =
                R.[RoleCode]

        FROM [Security].[Users] AS U

        INNER JOIN [Security].[Roles] AS R
            ON R.[RoleId] = U.[RoleId]

        WHERE U.[UserId] = @CreatedByUserId;

        IF @ActorEmailAddress IS NULL
        BEGIN
            THROW 52105,
                N'The creator user was not found.',
                1;
        END;

        /*
            Business uniqueness validations.

            The UNIQUE constraints in Departments remain
            the final database protection.
        */

        IF EXISTS
        (
            SELECT 1
            FROM [HumanResources].[Departments]
                WITH (UPDLOCK, HOLDLOCK)
            WHERE [DepartmentCode] =
                @NormalizedDepartmentCode
        )
        BEGIN
            THROW 52106,
                N'A department with the specified code already exists.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [HumanResources].[Departments]
                WITH (UPDLOCK, HOLDLOCK)
            WHERE [Name] =
                @NormalizedName
        )
        BEGIN
            THROW 52107,
                N'A department with the specified name already exists.',
                1;
        END;

        /*
            Create department.
        */

        INSERT INTO [HumanResources].[Departments]
        (
            [DepartmentCode],
            [Name],
            [Description],
            [IsActive],
            [CreatedAtUtc],
            [CreatedByUserId]
        )
        VALUES
        (
            @NormalizedDepartmentCode,
            @NormalizedName,
            @NormalizedDescription,
            1,
            @OccurredAtUtc,
            @CreatedByUserId
        );

        SET @DepartmentId =
            CONVERT(
                int,
                SCOPE_IDENTITY()
            );

        /*
            Values stored in the audit entry.
        */

        SET @NewValuesJson =
        (
            SELECT
                @NormalizedDepartmentCode
                    AS [DepartmentCode],

                @NormalizedName
                    AS [Name],

                @NormalizedDescription
                    AS [Description],

                CAST(1 AS bit)
                    AS [IsActive]

            FOR JSON PATH,
                INCLUDE_NULL_VALUES,
                WITHOUT_ARRAY_WRAPPER
        );

        /*
            Audit the administrative action.
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
            @CreatedByUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'The department was created successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            @NewValuesJson,
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        /*
            According to our database standard,
            Create operations return the generated ID.
        */

        SELECT
            @DepartmentId AS [DepartmentId];
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