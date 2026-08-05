CREATE PROCEDURE [Security].[CreatePasswordResetToken]
    @EmailAddress nvarchar(254),
    @TokenHash varbinary(32),
    @ExpiresAtUtc datetime2(3),
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

    DECLARE @NormalizedEmailAddress nvarchar(254) =
        LOWER(LTRIM(RTRIM(@EmailAddress)));

    DECLARE @UserId int = NULL;
    DECLARE @StoredEmailAddress nvarchar(254) = NULL;
    DECLARE @PasswordResetTokenId int = NULL;

    IF @NormalizedEmailAddress IS NULL
       OR @NormalizedEmailAddress = N''
    BEGIN
        THROW 51040, 'Email address is required.', 1;
    END;

    IF @TokenHash IS NULL
       OR DATALENGTH(@TokenHash) <> 32
    BEGIN
        THROW 51041, 'Token hash must contain exactly 32 bytes.', 1;
    END;

    IF @ExpiresAtUtc IS NULL
       OR @ExpiresAtUtc <= @OccurredAtUtc
    BEGIN
        THROW 51042, 'Token expiration must be later than the current UTC time.', 1;
    END;

    IF @CorrelationId IS NULL
       OR @CorrelationId =
          CONVERT(
              uniqueidentifier,
              '00000000-0000-0000-0000-000000000000'
          )
    BEGIN
        THROW 51043, 'CorrelationId is required.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        /*
            The lock on Security.Users serializes concurrent password-reset
            requests for the same account.

            A locked account is still allowed to request a reset because
            completing the reset will clear the failed attempts and lockout.

            An inactive account, inactive role, unconfirmed email or inactive
            related employee is not eligible.
        */
        SELECT
            @UserId = U.[UserId],
            @StoredEmailAddress = U.[EmailAddress]
        FROM [Security].[Users] AS U
            WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [Security].[Roles] AS R
            ON R.[RoleId] = U.[RoleId]
        LEFT JOIN [HumanResources].[Employees] AS E
            ON E.[UserId] = U.[UserId]
        WHERE
            U.[EmailAddress] = @NormalizedEmailAddress
            AND U.[IsActive] = 1
            AND U.[IsEmailConfirmed] = 1
            AND R.[IsActive] = 1
            AND
            (
                E.[EmployeeId] IS NULL
                OR E.[IsActive] = 1
            );

        IF @UserId IS NOT NULL
        BEGIN
            /*
                Revoke every token that has not already been used or revoked.
                Expired tokens are also revoked to leave a clear final state.
            */
            UPDATE [Security].[PasswordResetTokens]
            SET
                [RevokedAtUtc] = @OccurredAtUtc
            WHERE
                [UserId] = @UserId
                AND [UsedAtUtc] IS NULL
                AND [RevokedAtUtc] IS NULL;

            INSERT INTO [Security].[PasswordResetTokens]
            (
                [UserId],
                [TokenHash],
                [ExpiresAtUtc],
                [UsedAtUtc],
                [RevokedAtUtc],
                [CreatedAtUtc],
                [CorrelationId]
            )
            VALUES
            (
                @UserId,
                @TokenHash,
                @ExpiresAtUtc,
                NULL,
                NULL,
                @OccurredAtUtc,
                @CorrelationId
            );

            SET @PasswordResetTokenId =
                CONVERT(int, SCOPE_IDENTITY());
        END;

        /*
            The request is audited generically whether the email belongs
            to an eligible account or not.

            No original token, token hash or password is included.
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
            N'PasswordResetRequested',
            N'User',
            CASE
                WHEN @UserId IS NULL
                    THEN NULL
                ELSE CONVERT(nvarchar(100), @UserId)
            END,
            N'Anonymous',
            NULL,
            COALESCE(
                @StoredEmailAddress,
                @NormalizedEmailAddress
            ),
            NULL,
            1,
            N'Password reset request received.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        /*
            This result is for internal Application/Infrastructure use.
            It must never be returned directly by the public endpoint.
        */
        SELECT
            [PasswordResetTokenId] =
                @PasswordResetTokenId,
            [UserId] =
                @UserId,
            [EmailAddress] =
                @StoredEmailAddress,
            [ExpiresAtUtc] =
                CASE
                    WHEN @PasswordResetTokenId IS NULL
                        THEN CAST(NULL AS datetime2(3))
                    ELSE @ExpiresAtUtc
                END,
            [WasCreated] =
                CONVERT(
                    bit,
                    CASE
                        WHEN @PasswordResetTokenId IS NULL
                            THEN 0
                        ELSE 1
                    END
                );
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;
    END CATCH;
END;