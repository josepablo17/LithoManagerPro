CREATE PROCEDURE [Security].[RotateRefreshToken]
    @CurrentTokenHash varbinary(32),
    @NewTokenHash varbinary(32),
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

    DECLARE @CurrentRefreshTokenId int = NULL;
    DECLARE @NewRefreshTokenId int = NULL;
    DECLARE @UserId int = NULL;
    DECLARE @TokenFamilyId uniqueidentifier = NULL;
    DECLARE @CurrentExpiresAtUtc datetime2(3) = NULL;
    DECLARE @CurrentReplacedAtUtc datetime2(3) = NULL;
    DECLARE @CurrentRevokedAtUtc datetime2(3) = NULL;
    DECLARE @CurrentTokenVersion int = NULL;

    DECLARE @EmailAddress nvarchar(254) = NULL;
    DECLARE @RoleCode nvarchar(50) = NULL;
    DECLARE @TokenVersion int = NULL;
    DECLARE @IsEmailConfirmed bit = NULL;
    DECLARE @IsUserActive bit = NULL;
    DECLARE @IsRoleActive bit = NULL;
    DECLARE @EmployeeId int = NULL;
    DECLARE @IsEmployeeActive bit = NULL;
    DECLARE @DepartmentId int = NULL;
    DECLARE @IsDepartmentActive bit = NULL;

    DECLARE @WasRotated bit = 0;
    DECLARE @FailureReason nvarchar(100) = NULL;

    IF @CurrentTokenHash IS NULL
       OR DATALENGTH(@CurrentTokenHash) <> 32
    BEGIN
        THROW 51340,
            N'Current token hash must contain exactly 32 bytes.',
            1;
    END;

    IF @NewTokenHash IS NULL
       OR DATALENGTH(@NewTokenHash) <> 32
    BEGIN
        THROW 51341,
            N'New token hash must contain exactly 32 bytes.',
            1;
    END;

    IF @CurrentTokenHash = @NewTokenHash
    BEGIN
        THROW 51342,
            N'The new token hash must be different from the current token hash.',
            1;
    END;

    IF @ExpiresAtUtc IS NULL
       OR @ExpiresAtUtc <= @OccurredAtUtc
    BEGIN
        THROW 51343,
            N'Token expiration must be later than the current UTC time.',
            1;
    END;

    IF @CorrelationId IS NULL
       OR @CorrelationId =
            CONVERT(
                uniqueidentifier,
                '00000000-0000-0000-0000-000000000000'
            )
    BEGIN
        THROW 51344,
            N'CorrelationId is required.',
            1;
    END;

    /*
        Resolve the token identity before taking update locks.
        The locked read inside the transaction validates the
        token state again before any mutation is accepted.
    */
    SELECT
        @CurrentRefreshTokenId =
            T.[RefreshTokenId],

        @UserId =
            T.[UserId],

        @TokenFamilyId =
            T.[TokenFamilyId]

    FROM [Security].[RefreshTokens] AS T
    WHERE T.[TokenHash] = @CurrentTokenHash;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @CurrentRefreshTokenId IS NULL
        BEGIN
            SET @FailureReason = N'InvalidToken';

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
                N'RefreshTokenRotationFailed',
                N'RefreshTokens',
                NULL,
                N'Anonymous',
                NULL,
                NULL,
                NULL,
                0,
                N'Refresh token rotation failed.',
                @ClientIpAddress,
                @UserAgent,
                N'POST',
                @RequestPath,
                N'The refresh token was not found.',
                @OccurredAtUtc
            );
        END
        ELSE
        BEGIN
            SELECT
                @EmailAddress =
                    U.[EmailAddress],

                @RoleCode =
                    R.[RoleCode],

                @TokenVersion =
                    U.[TokenVersion],

                @IsEmailConfirmed =
                    U.[IsEmailConfirmed],

                @IsUserActive =
                    U.[IsActive],

                @IsRoleActive =
                    R.[IsActive],

                @EmployeeId =
                    E.[EmployeeId],

                @IsEmployeeActive =
                    E.[IsActive],

                @DepartmentId =
                    D.[DepartmentId],

                @IsDepartmentActive =
                    D.[IsActive]

            FROM [Security].[Users] AS U
                WITH (UPDLOCK, HOLDLOCK)

            INNER JOIN [Security].[Roles] AS R
                ON R.[RoleId] = U.[RoleId]

            LEFT JOIN [HumanResources].[Employees] AS E
                ON E.[UserId] = U.[UserId]

            LEFT JOIN [HumanResources].[Departments] AS D
                ON D.[DepartmentId] = E.[DepartmentId]

            WHERE U.[UserId] = @UserId;

            SELECT
                @CurrentExpiresAtUtc =
                    T.[ExpiresAtUtc],

                @CurrentReplacedAtUtc =
                    T.[ReplacedAtUtc],

                @CurrentRevokedAtUtc =
                    T.[RevokedAtUtc],

                @CurrentTokenVersion =
                    T.[TokenVersion]

            FROM [Security].[RefreshTokens] AS T
                WITH (UPDLOCK, HOLDLOCK)

            WHERE
                T.[RefreshTokenId] = @CurrentRefreshTokenId
                AND T.[TokenHash] = @CurrentTokenHash;

            IF @EmailAddress IS NULL
            BEGIN
                SET @FailureReason = N'UserNotFound';
            END
            ELSE IF @CurrentExpiresAtUtc IS NULL
            BEGIN
                SET @FailureReason = N'InvalidToken';
            END
            ELSE IF @CurrentReplacedAtUtc IS NOT NULL
                    OR @CurrentRevokedAtUtc IS NOT NULL
            BEGIN
                SET @FailureReason = N'ReuseDetected';

                UPDATE [Security].[RefreshTokens]
                SET
                    [RevokedAtUtc] =
                        @OccurredAtUtc,

                    [RevokedReason] =
                        N'RefreshTokenReuseDetected'

                WHERE
                    [UserId] = @UserId
                    AND [TokenFamilyId] = @TokenFamilyId
                    AND [ReplacedAtUtc] IS NULL
                    AND [RevokedAtUtc] IS NULL;
            END
            ELSE IF @CurrentExpiresAtUtc <= @OccurredAtUtc
            BEGIN
                SET @FailureReason = N'Expired';

                UPDATE [Security].[RefreshTokens]
                SET
                    [RevokedAtUtc] =
                        @OccurredAtUtc,

                    [RevokedReason] =
                        N'Expired'

                WHERE
                    [RefreshTokenId] = @CurrentRefreshTokenId
                    AND [ReplacedAtUtc] IS NULL
                    AND [RevokedAtUtc] IS NULL;
            END
            ELSE IF @CurrentTokenVersion <> @TokenVersion
            BEGIN
                SET @FailureReason = N'TokenVersionMismatch';

                UPDATE [Security].[RefreshTokens]
                SET
                    [RevokedAtUtc] =
                        @OccurredAtUtc,

                    [RevokedReason] =
                        N'TokenVersionMismatch'

                WHERE
                    [UserId] = @UserId
                    AND [ReplacedAtUtc] IS NULL
                    AND [RevokedAtUtc] IS NULL;
            END
            ELSE IF @IsUserActive = 0
                    OR @IsEmailConfirmed = 0
                    OR @IsRoleActive = 0
                    OR
                    (
                        @EmployeeId IS NOT NULL
                        AND @IsEmployeeActive = 0
                    )
                    OR
                    (
                        @DepartmentId IS NOT NULL
                        AND @IsDepartmentActive = 0
                    )
            BEGIN
                SET @FailureReason = N'AccountNotEligible';

                UPDATE [Security].[RefreshTokens]
                SET
                    [RevokedAtUtc] =
                        @OccurredAtUtc,

                    [RevokedReason] =
                        N'AccountNotEligible'

                WHERE
                    [UserId] = @UserId
                    AND [ReplacedAtUtc] IS NULL
                    AND [RevokedAtUtc] IS NULL;
            END
            ELSE
            BEGIN
                UPDATE [Security].[RefreshTokens]
                SET
                    [ReplacedAtUtc] =
                        @OccurredAtUtc,

                    [LastUsedAtUtc] =
                        @OccurredAtUtc,

                    [LastUsedByIpAddress] =
                        @ClientIpAddress,

                    [LastUsedByUserAgent] =
                        @UserAgent

                WHERE
                    [RefreshTokenId] = @CurrentRefreshTokenId
                    AND [ReplacedAtUtc] IS NULL
                    AND [RevokedAtUtc] IS NULL
                    AND [ExpiresAtUtc] > @OccurredAtUtc;

                IF @@ROWCOUNT <> 1
                BEGIN
                    THROW 51345,
                        N'The current refresh token could not be marked as replaced.',
                        1;
                END;

                INSERT INTO [Security].[RefreshTokens]
                (
                    [UserId],
                    [TokenHash],
                    [TokenFamilyId],
                    [TokenVersion],
                    [ReplacedByRefreshTokenId],
                    [ReplacedAtUtc],
                    [ExpiresAtUtc],
                    [RevokedAtUtc],
                    [RevokedReason],
                    [CreatedAtUtc],
                    [CreatedByIpAddress],
                    [CreatedByUserAgent],
                    [LastUsedAtUtc],
                    [LastUsedByIpAddress],
                    [LastUsedByUserAgent],
                    [CorrelationId]
                )
                VALUES
                (
                    @UserId,
                    @NewTokenHash,
                    @TokenFamilyId,
                    @TokenVersion,
                    NULL,
                    NULL,
                    @ExpiresAtUtc,
                    NULL,
                    NULL,
                    @OccurredAtUtc,
                    @ClientIpAddress,
                    @UserAgent,
                    NULL,
                    NULL,
                    NULL,
                    @CorrelationId
                );

                SET @NewRefreshTokenId =
                    CONVERT(int, SCOPE_IDENTITY());

                UPDATE [Security].[RefreshTokens]
                SET
                    [ReplacedByRefreshTokenId] =
                        @NewRefreshTokenId

                WHERE [RefreshTokenId] = @CurrentRefreshTokenId;

                IF @@ROWCOUNT <> 1
                BEGIN
                    THROW 51346,
                        N'The replaced refresh token could not be linked to the new token.',
                        1;
                END;

                SET @WasRotated = 1;
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
                CASE
                    WHEN @WasRotated = 1
                        THEN N'RefreshTokenRotated'
                    WHEN @FailureReason = N'ReuseDetected'
                        THEN N'RefreshTokenReuseDetected'
                    ELSE N'RefreshTokenRotationFailed'
                END,
                N'RefreshTokens',
                CONVERT(nvarchar(100), @CurrentRefreshTokenId),
                N'User',
                @UserId,
                @EmailAddress,
                @RoleCode,
                @WasRotated,
                CASE
                    WHEN @WasRotated = 1
                        THEN N'Refresh token rotated successfully.'
                    ELSE N'Refresh token rotation failed.'
                END,
                @ClientIpAddress,
                @UserAgent,
                N'POST',
                @RequestPath,
                CASE
                    WHEN @WasRotated = 1
                        THEN NULL
                    ELSE @FailureReason
                END,
                @OccurredAtUtc
            );
        END;

        COMMIT TRANSACTION;

        SELECT
            [CurrentRefreshTokenId] =
                CASE
                    WHEN @WasRotated = 1
                        THEN @CurrentRefreshTokenId
                    ELSE NULL
                END,

            [NewRefreshTokenId] =
                CASE
                    WHEN @WasRotated = 1
                        THEN @NewRefreshTokenId
                    ELSE NULL
                END,

            [UserId] =
                CASE
                    WHEN @WasRotated = 1
                        THEN @UserId
                    ELSE NULL
                END,

            [TokenFamilyId] =
                CASE
                    WHEN @WasRotated = 1
                        THEN @TokenFamilyId
                    ELSE NULL
                END,

            [ExpiresAtUtc] =
                CASE
                    WHEN @WasRotated = 1
                        THEN @ExpiresAtUtc
                    ELSE CAST(NULL AS datetime2(3))
                END,

            [RotatedAtUtc] =
                CASE
                    WHEN @WasRotated = 1
                        THEN @OccurredAtUtc
                    ELSE CAST(NULL AS datetime2(3))
                END,

            [WasRotated] =
                @WasRotated,

            [FailureReason] =
                @FailureReason;
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
