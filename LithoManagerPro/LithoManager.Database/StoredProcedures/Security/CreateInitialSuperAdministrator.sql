CREATE PROCEDURE [Security].[CreateInitialSuperAdministrator]
    @EmailAddress nvarchar(254),
    @PasswordHash nvarchar(500),
    @TemporaryPasswordExpiresAtUtc datetime2(3),
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
        COALESCE(@CorrelationId, NEWID());

    DECLARE @NormalizedEmailAddress nvarchar(254);

    SET @NormalizedEmailAddress =
        NULLIF(
            LTRIM(RTRIM(@EmailAddress)),
            N''
        );

    IF @NormalizedEmailAddress IS NULL
    BEGIN
        THROW 51020,
            N'EmailAddress is required.',
            1;
    END;

    IF @NormalizedEmailAddress LIKE N'% %'
    BEGIN
        THROW 51021,
            N'EmailAddress cannot contain spaces.',
            1;
    END;

    IF @PasswordHash IS NULL
       OR LEN(LTRIM(RTRIM(@PasswordHash))) = 0
    BEGIN
        THROW 51022,
            N'PasswordHash is required.',
            1;
    END;

    IF @TemporaryPasswordExpiresAtUtc <= @OccurredAtUtc
    BEGIN
        THROW 51023,
            N'TemporaryPasswordExpiresAtUtc must be in the future.',
            1;
    END;

    DECLARE @RoleId int;
    DECLARE @UserId int;

    BEGIN TRY
        BEGIN TRANSACTION;

        /*
            The locking hints prevent two bootstrap processes
            from creating initial administrators simultaneously.
        */

        IF EXISTS
        (
            SELECT 1
            FROM [Security].[Users]
                WITH (UPDLOCK, HOLDLOCK)
        )
        BEGIN
            THROW 51024,
                N'The initial administrator can only be created when no users exist.',
                1;
        END;

        SELECT
            @RoleId = R.[RoleId]
        FROM [Security].[Roles] AS R
            WITH (UPDLOCK, HOLDLOCK)
        WHERE R.[RoleCode] = N'SuperAdministrator'
          AND R.[IsActive] = 1;

        IF @RoleId IS NULL
        BEGIN
            THROW 51025,
                N'The active SuperAdministrator role was not found.',
                1;
        END;

        INSERT INTO [Security].[Users]
        (
            [RoleId],
            [EmailAddress],
            [PasswordHash],
            [IsEmailConfirmed],
            [IsActive],
            [RequiresPasswordChange],
            [TemporaryPasswordExpiresAtUtc],
            [PasswordChangedAtUtc],
            [FailedLoginAttempts],
            [LockoutEndAtUtc],
            [LastLoginAtUtc],
            [CreatedByUserId]
        )
        VALUES
        (
            @RoleId,
            @NormalizedEmailAddress,
            @PasswordHash,
            1,
            1,
            1,
            @TemporaryPasswordExpiresAtUtc,
            NULL,
            0,
            NULL,
            NULL,
            NULL
        );

        SET @UserId =
            CONVERT(int, SCOPE_IDENTITY());

        INSERT INTO [Audit].[AuditLogs]
        (
            [CorrelationId],
            [ModuleName],
            [ActionName],
            [EntityName],
            [EntityId],
            [ActorType],
            [ActorEmailAddress],
            [ActorRoleCode],
            [IsSuccessful],
            [EventDescription],
            [ClientIpAddress],
            [UserAgent],
            [HttpMethod],
            [RequestPath],
            [OccurredAtUtc]
        )
        VALUES
        (
            @ResolvedCorrelationId,
            N'Security',
            N'InitialSuperAdministratorCreated',
            N'Users',
            CONVERT(nvarchar(100), @UserId),
            N'System',
            @NormalizedEmailAddress,
            N'SuperAdministrator',
            1,
            N'Initial SuperAdministrator account created.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            U.[UserId],
            U.[EmailAddress],
            R.[RoleCode],
            U.[IsActive],
            U.[RequiresPasswordChange],
            U.[TemporaryPasswordExpiresAtUtc],
            U.[CreatedAtUtc]
        FROM [Security].[Users] AS U

        INNER JOIN [Security].[Roles] AS R
            ON R.[RoleId] = U.[RoleId]

        WHERE U.[UserId] = @UserId;
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