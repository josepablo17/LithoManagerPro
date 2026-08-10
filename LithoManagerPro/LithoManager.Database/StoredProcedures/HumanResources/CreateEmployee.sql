CREATE PROCEDURE [HumanResources].[CreateEmployee]
    @UserId int = NULL,
    @DepartmentId int,
    @IdentificationNumber nvarchar(4000),
    @FirstName nvarchar(4000),
    @LastName nvarchar(4000),
    @PhoneNumber nvarchar(4000) = NULL,
    @BirthDate date = NULL,
    @HireDate date,
    @TerminationDate date = NULL,
    @JobTitle nvarchar(4000),
    @BaseSalary decimal(18, 2),
    @ProfileImagePath nvarchar(4000) = NULL,
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

    DECLARE @NormalizedIdentificationNumber nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@IdentificationNumber)),
            N''
        );

    DECLARE @NormalizedFirstName nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@FirstName)),
            N''
        );

    DECLARE @NormalizedLastName nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@LastName)),
            N''
        );

    DECLARE @NormalizedPhoneNumber nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@PhoneNumber)),
            N''
        );

    DECLARE @NormalizedJobTitle nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@JobTitle)),
            N''
        );

    DECLARE @NormalizedProfileImagePath nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@ProfileImagePath)),
            N''
        );

    IF @UserId IS NOT NULL
       AND @UserId <= 0
    BEGIN
        THROW 52101,
            N'UserId must be greater than zero when provided.',
            1;
    END;

    IF @DepartmentId IS NULL
       OR @DepartmentId <= 0
    BEGIN
        THROW 52102,
            N'DepartmentId must be greater than zero.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 52103,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @NormalizedIdentificationNumber IS NULL
    BEGIN
        THROW 52104,
            N'IdentificationNumber is required.',
            1;
    END;

    IF LEN(@NormalizedIdentificationNumber) > 30
    BEGIN
        THROW 52105,
            N'IdentificationNumber cannot exceed 30 characters.',
            1;
    END;

    IF @NormalizedFirstName IS NULL
    BEGIN
        THROW 52106,
            N'FirstName is required.',
            1;
    END;

    IF LEN(@NormalizedFirstName) > 100
    BEGIN
        THROW 52107,
            N'FirstName cannot exceed 100 characters.',
            1;
    END;

    IF @NormalizedLastName IS NULL
    BEGIN
        THROW 52108,
            N'LastName is required.',
            1;
    END;

    IF LEN(@NormalizedLastName) > 150
    BEGIN
        THROW 52109,
            N'LastName cannot exceed 150 characters.',
            1;
    END;

    IF @NormalizedPhoneNumber IS NOT NULL
       AND LEN(@NormalizedPhoneNumber) > 25
    BEGIN
        THROW 52110,
            N'PhoneNumber cannot exceed 25 characters.',
            1;
    END;

    IF @HireDate IS NULL
    BEGIN
        THROW 52111,
            N'HireDate is required.',
            1;
    END;

    IF @TerminationDate IS NOT NULL
       AND @TerminationDate < @HireDate
    BEGIN
        THROW 52112,
            N'TerminationDate cannot be earlier than HireDate.',
            1;
    END;

    IF @NormalizedJobTitle IS NULL
    BEGIN
        THROW 52113,
            N'JobTitle is required.',
            1;
    END;

    IF LEN(@NormalizedJobTitle) > 100
    BEGIN
        THROW 52114,
            N'JobTitle cannot exceed 100 characters.',
            1;
    END;

    IF @BaseSalary IS NULL
       OR @BaseSalary < 0
    BEGIN
        THROW 52115,
            N'BaseSalary must be greater than or equal to zero.',
            1;
    END;

    IF @NormalizedProfileImagePath IS NOT NULL
       AND LEN(@NormalizedProfileImagePath) > 500
    BEGIN
        THROW 52116,
            N'ProfileImagePath cannot exceed 500 characters.',
            1;
    END;

    DECLARE @ActorEmailAddress nvarchar(254);
    DECLARE @ActorRoleCode nvarchar(50);
    DECLARE @IsActorUserActive bit;
    DECLARE @IsActorRoleActive bit;
    DECLARE @ActorEmployeeId int;
    DECLARE @IsActorEmployeeActive bit;
    DECLARE @IsActorDepartmentActive bit;

    DECLARE @DepartmentCode nvarchar(50);
    DECLARE @DepartmentName nvarchar(100);
    DECLARE @IsDepartmentActive bit;
    DECLARE @TargetUserEmailAddress nvarchar(254);
    DECLARE @EmployeeId int;

    DECLARE @CreatedEmployee TABLE
    (
        [EmployeeId] int NOT NULL,
        [UserId] int NULL,
        [DepartmentId] int NOT NULL,
        [IdentificationNumber] nvarchar(30) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(150) NOT NULL,
        [PhoneNumber] nvarchar(25) NULL,
        [BirthDate] date NULL,
        [HireDate] date NOT NULL,
        [TerminationDate] date NULL,
        [JobTitle] nvarchar(100) NOT NULL,
        [BaseSalary] decimal(18, 2) NOT NULL,
        [ProfileImagePath] nvarchar(500) NULL,
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
            THROW 52117,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 52118,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 52119,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 52120,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 52121,
                N'The actor department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator'
        )
        BEGIN
            THROW 52122,
                N'The actor role is not allowed to create employees.',
                1;
        END;

        SELECT
            @DepartmentCode =
                D.[DepartmentCode],
            @DepartmentName =
                D.[Name],
            @IsDepartmentActive =
                D.[IsActive]
        FROM [HumanResources].[Departments] AS D
            WITH (UPDLOCK, HOLDLOCK)
        WHERE D.[DepartmentId] = @DepartmentId;

        IF @DepartmentCode IS NULL
        BEGIN
            THROW 52123,
                N'The department was not found.',
                1;
        END;

        IF @IsDepartmentActive <> 1
        BEGIN
            THROW 52124,
                N'The department is inactive.',
                1;
        END;

        IF @UserId IS NOT NULL
        BEGIN
            SELECT
                @TargetUserEmailAddress =
                    U.[EmailAddress]
            FROM [Security].[Users] AS U
                WITH (UPDLOCK, HOLDLOCK)
            WHERE U.[UserId] = @UserId;

            IF @TargetUserEmailAddress IS NULL
            BEGIN
                THROW 52125,
                    N'The linked user was not found.',
                    1;
            END;

            IF EXISTS
            (
                SELECT 1
                FROM [HumanResources].[Employees] AS E
                    WITH (UPDLOCK, HOLDLOCK)
                WHERE E.[UserId] = @UserId
            )
            BEGIN
                THROW 52126,
                    N'The linked user is already assigned to another employee.',
                    1;
            END;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [HumanResources].[Employees] AS E
                WITH (UPDLOCK, HOLDLOCK)
            WHERE E.[IdentificationNumber] =
                @NormalizedIdentificationNumber
        )
        BEGIN
            THROW 52127,
                N'An employee with the same IdentificationNumber already exists.',
                1;
        END;

        INSERT INTO [HumanResources].[Employees]
        (
            [UserId],
            [DepartmentId],
            [IdentificationNumber],
            [FirstName],
            [LastName],
            [PhoneNumber],
            [BirthDate],
            [HireDate],
            [TerminationDate],
            [JobTitle],
            [BaseSalary],
            [ProfileImagePath],
            [IsActive],
            [CreatedByUserId]
        )
        OUTPUT
            INSERTED.[EmployeeId],
            INSERTED.[UserId],
            INSERTED.[DepartmentId],
            INSERTED.[IdentificationNumber],
            INSERTED.[FirstName],
            INSERTED.[LastName],
            INSERTED.[PhoneNumber],
            INSERTED.[BirthDate],
            INSERTED.[HireDate],
            INSERTED.[TerminationDate],
            INSERTED.[JobTitle],
            INSERTED.[BaseSalary],
            INSERTED.[ProfileImagePath],
            INSERTED.[IsActive],
            INSERTED.[CreatedAtUtc],
            INSERTED.[CreatedByUserId],
            INSERTED.[UpdatedAtUtc],
            INSERTED.[UpdatedByUserId],
            INSERTED.[RowVersion]
        INTO @CreatedEmployee
        (
            [EmployeeId],
            [UserId],
            [DepartmentId],
            [IdentificationNumber],
            [FirstName],
            [LastName],
            [PhoneNumber],
            [BirthDate],
            [HireDate],
            [TerminationDate],
            [JobTitle],
            [BaseSalary],
            [ProfileImagePath],
            [IsActive],
            [CreatedAtUtc],
            [CreatedByUserId],
            [UpdatedAtUtc],
            [UpdatedByUserId],
            [RowVersion]
        )
        VALUES
        (
            @UserId,
            @DepartmentId,
            @NormalizedIdentificationNumber,
            @NormalizedFirstName,
            @NormalizedLastName,
            @NormalizedPhoneNumber,
            @BirthDate,
            @HireDate,
            @TerminationDate,
            @NormalizedJobTitle,
            @BaseSalary,
            @NormalizedProfileImagePath,
            1,
            @ActorUserId
        );

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 52128,
                N'The employee insert returned an unexpected row count.',
                1;
        END;

        SELECT
            @EmployeeId =
                E.[EmployeeId]
        FROM @CreatedEmployee AS E;

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
            N'EmployeeCreated',
            N'Employees',
            CONVERT(
                nvarchar(100),
                @EmployeeId
            ),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Employee created successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            (
                SELECT
                    E.[EmployeeId],
                    E.[UserId],
                    E.[DepartmentId],
                    E.[IdentificationNumber],
                    E.[FirstName],
                    E.[LastName],
                    E.[PhoneNumber],
                    E.[BirthDate],
                    E.[HireDate],
                    E.[TerminationDate],
                    E.[JobTitle],
                    E.[BaseSalary],
                    E.[ProfileImagePath],
                    E.[IsActive]
                FROM @CreatedEmployee AS E
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            E.[EmployeeId],
            E.[UserId],
            @TargetUserEmailAddress AS [EmailAddress],
            E.[DepartmentId],
            @DepartmentCode AS [DepartmentCode],
            @DepartmentName AS [DepartmentName],
            @IsDepartmentActive AS [IsDepartmentActive],
            E.[IdentificationNumber],
            E.[FirstName],
            E.[LastName],
            E.[PhoneNumber],
            E.[BirthDate],
            E.[HireDate],
            E.[TerminationDate],
            E.[JobTitle],
            E.[BaseSalary],
            E.[ProfileImagePath],
            E.[IsActive],
            E.[CreatedAtUtc],
            E.[CreatedByUserId],
            E.[UpdatedAtUtc],
            E.[UpdatedByUserId],
            E.[RowVersion]
        FROM @CreatedEmployee AS E;
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
