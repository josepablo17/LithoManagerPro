CREATE PROCEDURE [HumanResources].[SetEmployeeStatus]
    @EmployeeId int,
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

    IF @EmployeeId IS NULL
       OR @EmployeeId <= 0
    BEGIN
        THROW 52181,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @IsActive IS NULL
    BEGIN
        THROW 52182,
            N'IsActive is required.',
            1;
    END;

    IF @ExpectedRowVersion IS NULL
       OR DATALENGTH(@ExpectedRowVersion) <> 8
    BEGIN
        THROW 52183,
            N'ExpectedRowVersion must contain exactly 8 bytes.',
            1;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 52184,
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

    DECLARE @ExistingUserId int;
    DECLARE @ExistingEmailAddress nvarchar(254);
    DECLARE @ExistingDepartmentId int;
    DECLARE @ExistingDepartmentCode nvarchar(50);
    DECLARE @ExistingDepartmentName nvarchar(100);
    DECLARE @ExistingIsDepartmentActive bit;
    DECLARE @ExistingIdentificationType nvarchar(30);
    DECLARE @ExistingIdentificationNumber nvarchar(30);
    DECLARE @ExistingFirstName nvarchar(100);
    DECLARE @ExistingLastName nvarchar(150);
    DECLARE @ExistingPhoneNumber nvarchar(8);
    DECLARE @ExistingBirthDate date;
    DECLARE @ExistingHireDate date;
    DECLARE @ExistingTerminationDate date;
    DECLARE @ExistingJobTitle nvarchar(100);
    DECLARE @ExistingBaseSalary decimal(18, 2);
    DECLARE @ExistingProfileImagePath nvarchar(500);
    DECLARE @ExistingIsActive bit;
    DECLARE @ExistingRowVersion varbinary(8);
    DECLARE @PreviousValuesJson nvarchar(max);

    DECLARE @ResultEmployee TABLE
    (
        [EmployeeId] int NOT NULL,
        [UserId] int NULL,
        [DepartmentId] int NOT NULL,
        [IdentificationType] nvarchar(30) NOT NULL,
        [IdentificationNumber] nvarchar(30) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(150) NOT NULL,
        [PhoneNumber] nvarchar(8) NULL,
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
            THROW 52185,
                N'The actor user was not found.',
                1;
        END;

        IF @IsActorUserActive = 0
        BEGIN
            THROW 52186,
                N'The actor user account is inactive.',
                1;
        END;

        IF @IsActorRoleActive = 0
        BEGIN
            THROW 52187,
                N'The actor role is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorEmployeeActive <> 1
        BEGIN
            THROW 52188,
                N'The actor employee record is inactive.',
                1;
        END;

        IF @ActorEmployeeId IS NOT NULL
           AND @IsActorDepartmentActive <> 1
        BEGIN
            THROW 52189,
                N'The actor department is inactive.',
                1;
        END;

        IF @ActorRoleCode NOT IN
        (
            N'SuperAdministrator',
            N'HumanResourcesAdministrator'
        )
        BEGIN
            THROW 52190,
                N'The actor role is not allowed to set employee status.',
                1;
        END;

        SELECT
            @ExistingUserId =
                E.[UserId],
            @ExistingEmailAddress =
                U.[EmailAddress],
            @ExistingDepartmentId =
                E.[DepartmentId],
            @ExistingDepartmentCode =
                D.[DepartmentCode],
            @ExistingDepartmentName =
                D.[Name],
            @ExistingIsDepartmentActive =
                D.[IsActive],
            @ExistingIdentificationType =
                E.[IdentificationType],
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
        LEFT JOIN [Security].[Users] AS U
            ON U.[UserId] = E.[UserId]
        INNER JOIN [HumanResources].[Departments] AS D
            ON D.[DepartmentId] = E.[DepartmentId]
        WHERE E.[EmployeeId] = @EmployeeId;

        IF @ExistingIdentificationNumber IS NULL
        BEGIN
            THROW 52191,
                N'The employee was not found.',
                1;
        END;

        IF @ExistingRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 52192,
                N'The employee has been modified by another transaction.',
                1;
        END;

        IF @IsActive = 1
           AND @ExistingIsDepartmentActive <> 1
        BEGIN
            THROW 52193,
                N'The employee cannot be activated while the department is inactive.',
                1;
        END;

        SET @PreviousValuesJson =
        (
            SELECT
                @EmployeeId AS [EmployeeId],
                @ExistingUserId AS [UserId],
                @ExistingDepartmentId AS [DepartmentId],
                @ExistingIdentificationType AS [IdentificationType],
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

        IF @ExistingIsActive = @IsActive
        BEGIN
            INSERT INTO @ResultEmployee
            (
                [EmployeeId],
                [UserId],
                [DepartmentId],
                [IdentificationType],
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
            SELECT
                E.[EmployeeId],
                E.[UserId],
                E.[DepartmentId],
                E.[IdentificationType],
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
            FROM [HumanResources].[Employees] AS E
            WHERE E.[EmployeeId] = @EmployeeId;
        END;
        ELSE
        BEGIN
            UPDATE [HumanResources].[Employees]
            SET
                [IsActive] =
                    @IsActive,
                [UpdatedAtUtc] =
                    @OccurredAtUtc,
                [UpdatedByUserId] =
                    @ActorUserId
            OUTPUT
                INSERTED.[EmployeeId],
                INSERTED.[UserId],
                INSERTED.[DepartmentId],
                INSERTED.[IdentificationType],
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
            INTO @ResultEmployee
            (
                [EmployeeId],
                [UserId],
                [DepartmentId],
                [IdentificationType],
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
                THROW 52194,
                    N'The employee status update returned an unexpected row count.',
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
            N'EmployeeStatusSet',
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
            CASE
                WHEN @ExistingIsActive = @IsActive
                    THEN N'Employee status was already set.'
                ELSE N'Employee status updated successfully.'
            END,
            @ClientIpAddress,
            @UserAgent,
            N'PATCH',
            @RequestPath,
            @PreviousValuesJson,
            (
                SELECT
                    E.[EmployeeId],
                    E.[UserId],
                    E.[DepartmentId],
                    E.[IdentificationType],
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
                FROM @ResultEmployee AS E
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
            ),
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            E.[EmployeeId],
            E.[UserId],
            @ExistingEmailAddress AS [EmailAddress],
            E.[DepartmentId],
            @ExistingDepartmentCode AS [DepartmentCode],
            @ExistingDepartmentName AS [DepartmentName],
            @ExistingIsDepartmentActive AS [IsDepartmentActive],
            E.[IdentificationType],
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
        FROM @ResultEmployee AS E;
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
