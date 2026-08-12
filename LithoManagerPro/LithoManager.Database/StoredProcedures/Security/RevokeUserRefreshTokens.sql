CREATE PROCEDURE [Security].[RevokeUserRefreshTokens]
    @UserId int,
    @CorrelationId uniqueidentifier,
    @RevokedReason nvarchar(100) = N'Logout',
    @ActorUserId int = NULL,
    @ClientIpAddress nvarchar(45) = NULL,
    @UserAgent nvarchar(512) = NULL,
    @RequestPath nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OccurredAtUtc datetime2(3) =
        SYSUTCDATETIME();

    DECLARE @EmailAddress nvarchar(254) = NULL;
    DECLARE @RoleCode nvarchar(50) = NULL;
    DECLARE @ActorEmailAddress nvarchar(254) = NULL;
    DECLARE @ActorRoleCode nvarchar(50) = NULL;
    DECLARE @RevokedCount int = 0;

    IF @UserId IS NULL
       OR @UserId <= 0
    BEGIN
        THROW 51380,
            N'UserId is required.',
            1;
    END;

    IF @ActorUserId IS NOT NULL
       AND @ActorUserId <= 0
    BEGIN
        THROW 51381,
            N'ActorUserId must be greater than zero when provided.',
            1;
    END;

    IF NULLIF(LTRIM(RTRIM(@RevokedReason)), N'') IS NULL
    BEGIN
        THROW 51382,
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
        THROW 51383,
            N'CorrelationId is required.',
            1;
    END;

    SET @RevokedReason =
        LTRIM(RTRIM(@RevokedReason));

    BEGIN TRY
        BEGIN TRANSACTION;

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

        IF @EmailAddress IS NULL
        BEGIN
            THROW 51384,
                N'The user was not found.',
                1;
        END;

        IF @ActorUserId IS NULL
        BEGIN
            SET @ActorUserId = @UserId;
            SET @ActorEmailAddress = @EmailAddress;
            SET @ActorRoleCode = @RoleCode;
        END
        ELSE
        BEGIN
            SELECT
                @ActorEmailAddress =
                    U.[EmailAddress],

                @ActorRoleCode =
                    R.[RoleCode]

            FROM [Security].[Users] AS U
                WITH (UPDLOCK, HOLDLOCK)

            INNER JOIN [Security].[Roles] AS R
                ON R.[RoleId] = U.[RoleId]

            WHERE U.[UserId] = @ActorUserId;

            IF @ActorEmailAddress IS NULL
            BEGIN
                THROW 51385,
                    N'The actor user was not found.',
                    1;
            END;
        END;

        UPDATE [Security].[RefreshTokens]
        SET
            [RevokedAtUtc] =
                @OccurredAtUtc,

            [RevokedReason] =
                @RevokedReason

        WHERE
            [UserId] = @UserId
            AND [ReplacedAtUtc] IS NULL
            AND [RevokedAtUtc] IS NULL;

        SET @RevokedCount =
            @@ROWCOUNT;

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
            @CorrelationId,
            N'Security',
            N'UserRefreshTokensRevoked',
            N'Users',
            CONVERT(nvarchar(100), @UserId),
            N'User',
            @ActorUserId,
            @ActorEmailAddress,
            @ActorRoleCode,
            1,
            N'Active refresh sessions were revoked for the user.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            CONCAT(
                N'{"targetUserId":',
                @UserId,
                N',"revokedCount":',
                @RevokedCount,
                N',"revokedReason":"',
                STRING_ESCAPE(@RevokedReason, 'json'),
                N'"}'
            ),
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            [UserId] =
                @UserId,

            [RevokedAtUtc] =
                CASE
                    WHEN @RevokedCount > 0
                        THEN @OccurredAtUtc
                    ELSE CAST(NULL AS datetime2(3))
                END,

            [RevokedCount] =
                @RevokedCount,

            [WasRevoked] =
                CONVERT(
                    bit,
                    CASE
                        WHEN @RevokedCount > 0
                            THEN 1
                        ELSE 0
                    END
                );
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
