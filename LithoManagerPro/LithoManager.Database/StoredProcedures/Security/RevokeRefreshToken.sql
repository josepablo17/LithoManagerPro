CREATE PROCEDURE [Security].[RevokeRefreshToken]
    @TokenHash varbinary(32),
    @CorrelationId uniqueidentifier,
    @RevokedReason nvarchar(100) = N'Logout',
    @ClientIpAddress nvarchar(45) = NULL,
    @UserAgent nvarchar(512) = NULL,
    @RequestPath nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OccurredAtUtc datetime2(3) =
        SYSUTCDATETIME();

    DECLARE @RefreshTokenId int = NULL;
    DECLARE @UserId int = NULL;
    DECLARE @TokenFamilyId uniqueidentifier = NULL;
    DECLARE @ReplacedAtUtc datetime2(3) = NULL;
    DECLARE @RevokedAtUtc datetime2(3) = NULL;
    DECLARE @EmailAddress nvarchar(254) = NULL;
    DECLARE @RoleCode nvarchar(50) = NULL;

    DECLARE @WasRevoked bit = 0;
    DECLARE @WasAlreadyInactive bit = 0;

    IF @TokenHash IS NULL
       OR DATALENGTH(@TokenHash) <> 32
    BEGIN
        THROW 51370,
            N'Token hash must contain exactly 32 bytes.',
            1;
    END;

    IF NULLIF(LTRIM(RTRIM(@RevokedReason)), N'') IS NULL
    BEGIN
        THROW 51371,
            N'RevokedReason is required.',
            1;
    END;

    IF @CorrelationId IS NULL
       OR @CorrelationId =
            CONVERT(
                uniqueidentifier,
                '00000000-0000-0000-0000-000000000000'
            )
    BEGIN
        THROW 51372,
            N'CorrelationId is required.',
            1;
    END;

    SET @RevokedReason =
        LTRIM(RTRIM(@RevokedReason));

    SELECT
        @RefreshTokenId =
            T.[RefreshTokenId],

        @UserId =
            T.[UserId],

        @TokenFamilyId =
            T.[TokenFamilyId]

    FROM [Security].[RefreshTokens] AS T
    WHERE T.[TokenHash] = @TokenHash;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @RefreshTokenId IS NOT NULL
           AND @UserId IS NOT NULL
        BEGIN
            SELECT
                @EmailAddress =
                    U.[EmailAddress],

                @RoleCode =
                    R.[RoleCode]

            FROM [Security].[Users] AS U
                WITH (UPDLOCK, HOLDLOCK)

            INNER JOIN [Security].[Roles] AS R
                ON R.[RoleId] = U.[RoleId]

            WHERE U.[UserId] = @UserId;

            SELECT
                @ReplacedAtUtc =
                    T.[ReplacedAtUtc],

                @RevokedAtUtc =
                    T.[RevokedAtUtc]

            FROM [Security].[RefreshTokens] AS T
                WITH (UPDLOCK, HOLDLOCK)

            WHERE
                T.[RefreshTokenId] = @RefreshTokenId
                AND T.[TokenHash] = @TokenHash;

            IF @ReplacedAtUtc IS NULL
               AND @RevokedAtUtc IS NULL
            BEGIN
                UPDATE [Security].[RefreshTokens]
                SET
                    [RevokedAtUtc] =
                        @OccurredAtUtc,

                    [RevokedReason] =
                        @RevokedReason

                WHERE
                    [RefreshTokenId] = @RefreshTokenId
                    AND [ReplacedAtUtc] IS NULL
                    AND [RevokedAtUtc] IS NULL;

                IF @@ROWCOUNT <> 1
                BEGIN
                    THROW 51373,
                        N'The refresh token could not be revoked.',
                        1;
                END;

                SET @WasRevoked = 1;
            END
            ELSE
            BEGIN
                SET @WasAlreadyInactive = 1;
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
            [ErrorMessage],
            [OccurredAtUtc]
        )
        VALUES
        (
            @CorrelationId,
            N'Security',
            N'RefreshTokenRevoked',
            N'RefreshTokens',
            CASE
                WHEN @RefreshTokenId IS NULL
                    THEN NULL
                ELSE CONVERT(nvarchar(100), @RefreshTokenId)
            END,
            CASE
                WHEN @UserId IS NULL
                    THEN N'Anonymous'
                ELSE N'User'
            END,
            @UserId,
            @EmailAddress,
            @RoleCode,
            CONVERT(bit, 1),
            N'Refresh token revocation requested.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            CASE
                WHEN @RefreshTokenId IS NULL
                    THEN N'The refresh token was not found.'
                WHEN @WasAlreadyInactive = 1
                    THEN N'The refresh token was already inactive.'
                ELSE NULL
            END,
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            [RefreshTokenId] =
                CASE
                    WHEN @WasRevoked = 1
                        THEN @RefreshTokenId
                    ELSE NULL
                END,

            [UserId] =
                CASE
                    WHEN @WasRevoked = 1
                        THEN @UserId
                    ELSE NULL
                END,

            [TokenFamilyId] =
                CASE
                    WHEN @WasRevoked = 1
                        THEN @TokenFamilyId
                    ELSE NULL
                END,

            [RevokedAtUtc] =
                CASE
                    WHEN @WasRevoked = 1
                        THEN @OccurredAtUtc
                    ELSE CAST(NULL AS datetime2(3))
                END,

            [WasRevoked] =
                @WasRevoked,

            [WasAlreadyInactive] =
                @WasAlreadyInactive;
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
