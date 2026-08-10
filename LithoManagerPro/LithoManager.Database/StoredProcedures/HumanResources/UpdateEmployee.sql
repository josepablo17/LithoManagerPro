CREATE PROCEDURE [HumanResources].[UpdateEmployee]
    @EmployeeId int,
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

    IF @EmployeeId IS NULL
       OR @EmployeeId <= 0
    BEGIN
        THROW 52141,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @UserId IS NOT NULL
       AND @UserId <= 0
    BEGIN
        THROW 52142,
            N'UserId must be greater than zero when provided.',
            1;
    END;

    IF @DepartmentId IS NULL
       OR @DepartmentId <= 0
    BEGIN
        THROW 52143,
            N'DepartmentId must be greater than zero.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
       OR DATALENGTH(@ExpectedRowVersion) <> 8
    BEGIN
        THROW 52144,
            N'ExpectedRowVersion must contain exactly 8 bytes.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 52145,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @NormalizedIdentificationNumber IS NULL
    BEGIN
        THROW 52146,
            N'IdentificationNumber is required.',
            1;
    END;

    IF LEN(@NormalizedIdentificationNumber) > 30
    BEGIN
        THROW 52147,
            N'IdentificationNumber cannot exceed 30 characters.',
            1;
    END;

    IF @NormalizedFirstName IS NULL
    BEGIN
        THROW 52148,
            N'FirstName is required.',
            1;
    END;

    IF LEN(@NormalizedFirstName) > 100
    BEGIN
        THROW 52149,
            N'FirstName cannot exceed 100 characters.',
            1;
    END;

    IF @NormalizedLastName IS NULL
    BEGIN
        THROW 52150,
            N'LastName is required.',
            1;
    END;

    IF LEN(@NormalizedLastName) > 150
    BEGIN
        THROW 52151,
            N'LastName cannot exceed 150 characters.',
            1;
    END;

    IF @NormalizedPhoneNumber IS NOT NULL
       AND LEN(@NormalizedPhoneNumber) > 25
    BEGIN
        THROW 52152,
            N'PhoneNumber cannot exceed 25 characters.',
            1;
    END;

    IF @HireDate IS NULL
    BEGIN
        THROW 52153,
            N'HireDate is required.',
            1;
    END;

    IF @TerminationDate IS NOT NULL
       AND @TerminationDate < @HireDate
    BEGIN
        THROW 52154,
            N'TerminationDate cannot be earlier than HireDate.',
            1;
    END;

    IF @NormalizedJobTitle IS NULL
    BEGIN
        THROW 52155,
            N'JobTitle is required.',
            1;
    END;

    IF LEN(@NormalizedJobTitle) > 100
    BEGIN
        THROW 52156,
            N'JobTitle cannot exceed 100 characters.',
            1;
    END;

    IF @BaseSalary IS NULL
       OR @BaseSalary < 0
    BEGIN
        THROW 52157,
            N'BaseSalary must be greater than or equal to zero.',
            1;
    END;

    IF @NormalizedProfileImagePath IS NOT NULL
       AND LEN(@NormalizedProfileImagePath) > 500
    BEGIN
        THROW 52158,
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
    DECLARE @PreviousValuesJson nvarchar(max);

    DECLARE @ExistingUserId int;
    DECLARE @ExistingDepartmentId int;
    DECLARE @ExistingIdentificationNumber nvarchar(30);
    DECLARE @ExistingFirstName nvarchar(100);
    DECLARE @ExistingLastName nvarchar(150);
    DECLARE @ExistingPhoneNumber nvarchar(25);
    DECLARE @ExistingBirthDate date;
    DECLARE @ExistingHireDate date;
    DECLARE @ExistingTerminationDate date;
    DECLARE @ExistingJobTitle nvarchar(100);
    DECLARE @ExistingBaseSalary decimal(18, 2);
    DECLARE @ExistingProfileImagePath nvarchar(500);
    DECLARE @ExistingIsActive bit;
    DECLARE @ExistingRowVersion varbinary(8);

    DECLARE @UpdatedEmployee TABLE
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
            THROW 52159,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 52160,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 52161,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 52162,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 52163,
                N'The actor department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator'
        )
        BEGIN
            THROW 52164,
                N'The actor role is not allowed to update employees.',
                1;
        END;

        SELECT
            @ExistingUserId =
                E.[UserId],
            @ExistingDepartmentId =
                E.[DepartmentId],
            @ExistingIdentificationNumber =
                E.[IdentificationNumber],
            @ExistingFirstName =
                E.[FirstName],
            @ExistingLastName =
                E.[LastName],
            @ExistingPhoneNumber =
                E.[PhoneNumber],
            @ExistingBirthDate =
                E.[BirthDate],
            @ExistingHireDate =
                E.[HireDate],
            @ExistingTerminationDate =
                E.[TerminationDate],
            @ExistingJobTitle =
                E.[JobTitle],
            @ExistingBaseSalary =
                E.[BaseSalary],
            @ExistingProfileImagePath =
                E.[ProfileImagePath],
            @ExistingIsActive =
                E.[IsActive],
            @ExistingRowVersion =
                E.[RowVersion]
        FROM [HumanResources].[Employees] AS E
            WITH (UPDLOCK, HOLDLOCK)
        WHERE E.[EmployeeId] = @EmployeeId;

        IF @ExistingIdentificationNumber IS NULL
        BEGIN
            THROW 52165,
                N'The employee was not found.',
                1;
        END;

        IF @ExistingRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 52166,
                N'The employee has been modified by another transaction.',
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
            THROW 52167,
                N'The department was not found.',
                1;
        END;

        IF @IsDepartmentActive <> 1
        BEGIN
            THROW 52168,
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
                THROW 52169,
                    N'The linked user was not found.',
                    1;
            END;

            IF EXISTS
            (
                SELECT 1
                FROM [HumanResources].[Employees] AS E
                    WITH (UPDLOCK, HOLDLOCK)
                WHERE E.[UserId] = @UserId
                  AND E.[EmployeeId] <> @EmployeeId
            )
            BEGIN
                THROW 52170,
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
              AND E.[EmployeeId] <> @EmployeeId
        )
        BEGIN
            THROW 52171,
                N'An employee with the same IdentificationNumber already exists.',
                1;
        END;

        SET @PreviousValuesJson =
        (
            SELECT
                @EmployeeId AS [EmployeeId],
                @ExistingUserId AS [UserId],
                @ExistingDepartmentId AS [DepartmentId],
                @ExistingIdentificationNumber AS [IdentificationNumber],
                @ExistingFirstName AS [FirstName],
                @ExistingLastName AS [LastName],
                @ExistingPhoneNumber AS [PhoneNumber],
                @ExistingBirthDate AS [BirthDate],
                @ExistingHireDate AS [HireDate],
                @ExistingTerminationDate AS [TerminationDate],
                @ExistingJobTitle AS [JobTitle],
                @ExistingBaseSalary AS [BaseSalary],
                @ExistingProfileImagePath AS [ProfileImagePath],
                @ExistingIsActive AS [IsActive]
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        UPDATE [HumanResources].[Employees]
        SET
            [UserId] =
                @UserId,
            [DepartmentId] =
                @DepartmentId,
            [IdentificationNumber] =
                @NormalizedIdentificationNumber,
            [FirstName] =
                @NormalizedFirstName,
            [LastName] =
                @NormalizedLastName,
            [PhoneNumber] =
                @NormalizedPhoneNumber,
            [BirthDate] =
                @BirthDate,
            [HireDate] =
                @HireDate,
            [TerminationDate] =
                @TerminationDate,
            [JobTitle] =
                @NormalizedJobTitle,
            [BaseSalary] =
                @BaseSalary,
            [ProfileImagePath] =
                @NormalizedProfileImagePath,
            [UpdatedAtUtc] =
                @OccurredAtUtc,
            [UpdatedByUserId] =
                @ActorUserId
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
        INTO @UpdatedEmployee
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
        WHERE [EmployeeId] = @EmployeeId;

        IF @@ROWCOUNT <> 1
        BEGIN
            THROW 52172,
                N'The employee update returned an unexpected row count.',
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
            N'EmployeeUpdated',
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
            N'Employee updated successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'PUT',
            @RequestPath,
            @PreviousValuesJson,
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
                FROM @UpdatedEmployee AS E
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
        FROM @UpdatedEmployee AS E;
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
