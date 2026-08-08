CREATE PROCEDURE
    [Security].[CompletePasswordReset]
    @TokenHash varbinary(32),
    @ExpectedPasswordHash nvarchar(500),
    @NewPasswordHash nvarchar(500),
    @CorrelationId uniqueidentifier,
    @ClientIpAddress nvarchar(45) = NULL,
    @UserAgent nvarchar(512) = NULL,
    @RequestPath nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OccurredAtUtc datetime2(3) =
        SYSUTCDATETIME();

    DECLARE @PasswordResetTokenId int = NULL;
    DECLARE @UserId int = NULL;
    DECLARE @TokenUserId int = NULL;

    DECLARE @ExpiresAtUtc datetime2(3) = NULL;
    DECLARE @UsedAtUtc datetime2(3) = NULL;
    DECLARE @RevokedAtUtc datetime2(3) = NULL;

    DECLARE @EmailAddress nvarchar(254) = NULL;
    DECLARE @RoleCode nvarchar(50) = NULL;
    DECLARE @CurrentPasswordHash nvarchar(500) = NULL;

    DECLARE @IsEmailConfirmed bit = NULL;
    DECLARE @IsUserActive bit = NULL;
    DECLARE @IsRoleActive bit = NULL;

    DECLARE @EmployeeId int = NULL;
    DECLARE @IsEmployeeActive bit = NULL;

    DECLARE @WasCompleted bit = 0;

    IF @TokenHash IS NULL
       OR DATALENGTH(@TokenHash) <> 32
    BEGIN
        THROW 51061,
            N'Token hash must contain exactly 32 bytes.',
            1;
    END;

    IF NULLIF(
        LTRIM(RTRIM(@ExpectedPasswordHash)),
        N'') IS NULL
    BEGIN
        THROW 51062,
            N'The expected password hash is required.',
            1;
    END;

    IF NULLIF(
        LTRIM(RTRIM(@NewPasswordHash)),
        N'') IS NULL
    BEGIN
        THROW 51063,
            N'The new password hash is required.',
            1;
    END;

    IF @CorrelationId IS NULL
       OR @CorrelationId =
            CONVERT(
                uniqueidentifier,
                '00000000-0000-0000-0000-000000000000'
            )
    BEGIN
        THROW 51064,
            N'CorrelationId is required.',
            1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        /*
            Resolve the token identity first.

            This lookup does not determine whether the
            reset will succeed. Every security-sensitive
            condition is checked again below while the
            affected rows are locked.
        */
        SELECT
            @PasswordResetTokenId =
                T.[PasswordResetTokenId],

            @UserId =
                T.[UserId]

        FROM [Security].[PasswordResetTokens] AS T

        WHERE
            T.[TokenHash] = @TokenHash;

        IF @PasswordResetTokenId IS NOT NULL
           AND @UserId IS NOT NULL
        BEGIN
            /*
                Lock the user first.

                This follows the same logical locking
                order used by password-reset creation:
                    Users -> PasswordResetTokens

                It also protects the password hash from
                changing between verification and update.
            */
            SELECT
                @EmailAddress =
                    U.[EmailAddress],

                @RoleCode =
                    R.[RoleCode],

                @CurrentPasswordHash =
                    U.[PasswordHash],

                @IsEmailConfirmed =
                    U.[IsEmailConfirmed],

                @IsUserActive =
                    U.[IsActive],

                @IsRoleActive =
                    R.[IsActive],

                @EmployeeId =
                    E.[EmployeeId],

                @IsEmployeeActive =
                    E.[IsActive]

            FROM [Security].[Users] AS U
                WITH (UPDLOCK, HOLDLOCK)

            INNER JOIN [Security].[Roles] AS R
                ON R.[RoleId] = U.[RoleId]

            LEFT JOIN [HumanResources].[Employees] AS E
                ON E.[UserId] = U.[UserId]

            WHERE
                U.[UserId] = @UserId;

            /*
                Now lock and re-read the token.

                This second validation is what makes the
                token truly single-use when concurrent
                reset requests arrive.
            */
            SELECT
                @TokenUserId =
                    T.[UserId],

                @ExpiresAtUtc =
                    T.[ExpiresAtUtc],

                @UsedAtUtc =
                    T.[UsedAtUtc],

                @RevokedAtUtc =
                    T.[RevokedAtUtc]

            FROM [Security].[PasswordResetTokens] AS T
                WITH (UPDLOCK, HOLDLOCK)

            WHERE
                T.[PasswordResetTokenId] =
                    @PasswordResetTokenId
                AND T.[TokenHash] =
                    @TokenHash;

            IF @TokenUserId = @UserId

               AND @ExpiresAtUtc IS NOT NULL
               AND @ExpiresAtUtc > @OccurredAtUtc

               AND @UsedAtUtc IS NULL
               AND @RevokedAtUtc IS NULL

               AND @EmailAddress IS NOT NULL
               AND @IsUserActive = 1
               AND @IsEmailConfirmed = 1
               AND @IsRoleActive = 1

               AND
               (
                   @EmployeeId IS NULL
                   OR @IsEmployeeActive = 1
               )

               /*
                   Use a byte-for-byte comparison instead
                   of the database's case-insensitive
                   string collation.

                   Password hashes are opaque security
                   values and must match exactly.
               */
               AND CONVERT(
                       varbinary(1000),
                       @CurrentPasswordHash
                   )
                   =
                   CONVERT(
                       varbinary(1000),
                       @ExpectedPasswordHash
                   )
            BEGIN
                UPDATE [Security].[Users]
                SET
                    [PasswordHash] =
                        @NewPasswordHash,

                    [RequiresPasswordChange] =
                        0,

                    [TemporaryPasswordExpiresAtUtc] =
                        NULL,

                    [PasswordChangedAtUtc] =
                        @OccurredAtUtc,

                    [FailedLoginAttempts] =
                        0,

                    [LockoutEndAtUtc] =
                        NULL,

                    [UpdatedAtUtc] =
                        @OccurredAtUtc,

                    [UpdatedByUserId] =
                        @UserId

                WHERE
                    [UserId] = @UserId;

                IF @@ROWCOUNT <> 1
                BEGIN
                    THROW 51065,
                        N'The password reset user update returned an unexpected row count.',
                        1;
                END;

                /*
                    Consume the exact token.

                    The extra conditions are defensive;
                    the token is already locked.
                */
                UPDATE [Security].[PasswordResetTokens]
                SET
                    [UsedAtUtc] =
                        @OccurredAtUtc

                WHERE
                    [PasswordResetTokenId] =
                        @PasswordResetTokenId
                    AND [UserId] =
                        @UserId
                    AND [UsedAtUtc] IS NULL
                    AND [RevokedAtUtc] IS NULL
                    AND [ExpiresAtUtc] >
                        @OccurredAtUtc;

                IF @@ROWCOUNT <> 1
                BEGIN
                    THROW 51066,
                        N'The password reset token could not be consumed.',
                        1;
                END;

                /*
                    Revoke every other token that might
                    still be active for this account.
                */
                UPDATE [Security].[PasswordResetTokens]
                SET
                    [RevokedAtUtc] =
                        @OccurredAtUtc

                WHERE
                    [UserId] = @UserId
                    AND [PasswordResetTokenId]
                        <> @PasswordResetTokenId
                    AND [UsedAtUtc] IS NULL
                    AND [RevokedAtUtc] IS NULL;

                /*
                    The reset was performed through an
                    anonymous recovery credential rather
                    than an authenticated access token.

                    Never store the plaintext reset token,
                    its hash, or either password in audit.
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
                    [OccurredAtUtc]
                )
                VALUES
                (
                    @CorrelationId,
                    N'Security',
                    N'PasswordResetCompleted',
                    N'Users',
                    CONVERT(
                        nvarchar(100),
                        @UserId
                    ),
                    N'Anonymous',
                    NULL,
                    @EmailAddress,
                    NULL,
                    1,
                    N'Password reset completed successfully.',
                    @ClientIpAddress,
                    @UserAgent,
                    N'POST',
                    @RequestPath,
                    @OccurredAtUtc
                );

                SET @WasCompleted = 1;
            END;
        END;

        COMMIT TRANSACTION;

        /*
            Return detailed identifiers only when the
            reset actually succeeded.

            An unavailable token always produces the
            same internal failure shape regardless of
            the exact reason.
        */
        SELECT
            [PasswordResetTokenId] =
                CASE
                    WHEN @WasCompleted = 1
                        THEN @PasswordResetTokenId
                    ELSE NULL
                END,

            [UserId] =
                CASE
                    WHEN @WasCompleted = 1
                        THEN @UserId
                    ELSE NULL
                END,

            [PasswordChangedAtUtc] =
                CASE
                    WHEN @WasCompleted = 1
                        THEN @OccurredAtUtc
                    ELSE CAST(NULL AS datetime2(3))
                END,

            [RequiresPasswordChange] =
                CASE
                    WHEN @WasCompleted = 1
                        THEN CONVERT(bit, 0)
                    ELSE CAST(NULL AS bit)
                END,

            [WasCompleted] =
                @WasCompleted;
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