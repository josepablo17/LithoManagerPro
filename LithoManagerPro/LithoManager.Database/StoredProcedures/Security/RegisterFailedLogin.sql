CREATE PROCEDURE [Security].[RegisterFailedLogin]
    @AttemptedEmailAddress nvarchar(254),
    @UserId int = NULL,
    @MaximumFailedAttempts smallint = 5,
    @LockoutDurationMinutes int = 15,
    @CorrelationId uniqueidentifier = NULL,
    @ClientIpAddress nvarchar(45) = NULL,
    @UserAgent nvarchar(512) = NULL,
    @RequestPath nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @MaximumFailedAttempts < 1
       OR @MaximumFailedAttempts > 20
    BEGIN
        THROW 51010,
            N'MaximumFailedAttempts must be between 1 and 20.',
            1;
    END;

    IF @LockoutDurationMinutes < 1
       OR @LockoutDurationMinutes > 1440
    BEGIN
        THROW 51011,
            N'LockoutDurationMinutes must be between 1 and 1440.',
            1;
    END;

    DECLARE @OccurredAtUtc datetime2(3) =
        SYSUTCDATETIME();

    DECLARE @ResolvedCorrelationId uniqueidentifier =
        COALESCE(@CorrelationId, NEWID());

    DECLARE @NormalizedEmailAddress nvarchar(254);

    SET @NormalizedEmailAddress =
        NULLIF(
            LTRIM(RTRIM(@AttemptedEmailAddress)),
            N''
        );

    IF @NormalizedEmailAddress IS NULL
    BEGIN
        THROW 51012,
            N'AttemptedEmailAddress is required.',
            1;
    END;

    DECLARE @RoleCode nvarchar(50);

    DECLARE @CurrentFailedLoginAttempts smallint = 0;
    DECLARE @CurrentLockoutEndAtUtc datetime2(3);

    DECLARE @NewFailedLoginAttempts smallint = 0;
    DECLARE @NewLockoutEndAtUtc datetime2(3);

    DECLARE @ShouldUpdateUser bit = 0;

    DECLARE @AdditionalDataJson nvarchar(max);

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @UserId IS NOT NULL
        BEGIN
            SELECT
                @NormalizedEmailAddress = U.[EmailAddress],
                @RoleCode = R.[RoleCode],
                @CurrentFailedLoginAttempts =
                    U.[FailedLoginAttempts],
                @CurrentLockoutEndAtUtc =
                    U.[LockoutEndAtUtc]
            FROM [Security].[Users] AS U
                WITH (UPDLOCK, HOLDLOCK)

            INNER JOIN [Security].[Roles] AS R
                ON R.[RoleId] = U.[RoleId]

            WHERE U.[UserId] = @UserId;

            IF @RoleCode IS NULL
            BEGIN
                THROW 51013,
                    N'The user was not found.',
                    1;
            END;

            /*
                If the account is already locked, the lockout
                time is not extended with every new attempt.
            */

            IF @CurrentLockoutEndAtUtc IS NOT NULL
               AND @CurrentLockoutEndAtUtc > @OccurredAtUtc
            BEGIN
                SET @NewFailedLoginAttempts =
                    @CurrentFailedLoginAttempts;

                SET @NewLockoutEndAtUtc =
                    @CurrentLockoutEndAtUtc;
            END;
            ELSE
            BEGIN
                DECLARE @BaseFailedLoginAttempts int;
                DECLARE @CalculatedFailedLoginAttempts int;

                SET @BaseFailedLoginAttempts =
                    CASE
                        WHEN @CurrentLockoutEndAtUtc IS NOT NULL
                             AND @CurrentLockoutEndAtUtc
                                 <= @OccurredAtUtc
                            THEN 0
                        ELSE @CurrentFailedLoginAttempts
                    END;

                SET @CalculatedFailedLoginAttempts =
                    @BaseFailedLoginAttempts + 1;

                IF @CalculatedFailedLoginAttempts > 32767
                BEGIN
                    SET @CalculatedFailedLoginAttempts = 32767;
                END;

                SET @NewFailedLoginAttempts =
                    CONVERT(
                        smallint,
                        @CalculatedFailedLoginAttempts
                    );

                IF @NewFailedLoginAttempts
                    >= @MaximumFailedAttempts
                BEGIN
                    SET @NewLockoutEndAtUtc =
                        DATEADD(
                            MINUTE,
                            @LockoutDurationMinutes,
                            @OccurredAtUtc
                        );
                END;
                ELSE
                BEGIN
                    SET @NewLockoutEndAtUtc = NULL;
                END;

                SET @ShouldUpdateUser = 1;
            END;

            IF @ShouldUpdateUser = 1
            BEGIN
                UPDATE [Security].[Users]
                SET
                    [FailedLoginAttempts] =
                        @NewFailedLoginAttempts,

                    [LockoutEndAtUtc] =
                        @NewLockoutEndAtUtc

                WHERE [UserId] = @UserId;
            END;
        END;

        SELECT
            @AdditionalDataJson =
            (
                SELECT
                    @NewFailedLoginAttempts
                        AS [FailedLoginAttempts],

                    @NewLockoutEndAtUtc
                        AS [LockoutEndAtUtc]

                FOR JSON PATH,
                WITHOUT_ARRAY_WRAPPER
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
            [AdditionalDataJson],
            [OccurredAtUtc]
        )
        VALUES
        (
            @ResolvedCorrelationId,
            N'Security',
            N'LoginFailed',
            CASE
                WHEN @UserId IS NULL
                    THEN NULL
                ELSE N'Users'
            END,
            CASE
                WHEN @UserId IS NULL
                    THEN NULL
                ELSE CONVERT(nvarchar(100), @UserId)
            END,
            N'Anonymous',
            @UserId,
            @NormalizedEmailAddress,
            @RoleCode,
            0,
            N'Authentication attempt failed.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            @AdditionalDataJson,
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            @UserId AS [UserId],

            @NewFailedLoginAttempts
                AS [FailedLoginAttempts],

            @NewLockoutEndAtUtc
                AS [LockoutEndAtUtc],

            CONVERT
            (
                bit,
                CASE
                    WHEN @NewLockoutEndAtUtc IS NOT NULL
                         AND @NewLockoutEndAtUtc
                             > @OccurredAtUtc
                        THEN 1
                    ELSE 0
                END
            ) AS [IsLockedOut];
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