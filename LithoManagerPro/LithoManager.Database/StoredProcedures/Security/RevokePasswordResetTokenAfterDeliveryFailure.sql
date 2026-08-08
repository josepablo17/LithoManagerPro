CREATE PROCEDURE
    [Security].[RevokePasswordResetTokenAfterDeliveryFailure]
    @PasswordResetTokenId int,
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

    DECLARE @UserId int = NULL;
    DECLARE @UsedAtUtc datetime2(3) = NULL;
    DECLARE @RevokedAtUtc datetime2(3) = NULL;

    DECLARE @WasRevoked bit = 0;
    DECLARE @IsInactive bit = 0;

    IF @PasswordResetTokenId <= 0
    BEGIN
        THROW 51050,
            N'PasswordResetTokenId must be greater than zero.',
            1;
    END;

    IF @CorrelationId IS NULL
       OR @CorrelationId =
            CONVERT(
                uniqueidentifier,
                '00000000-0000-0000-0000-000000000000'
            )
    BEGIN
        THROW 51051,
            N'CorrelationId is required.',
            1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        /*
            Lock the token while its state is checked
            and potentially changed.
        */
        SELECT
            @UserId =
                T.[UserId],

            @UsedAtUtc =
                T.[UsedAtUtc],

            @RevokedAtUtc =
                T.[RevokedAtUtc]

        FROM [Security].[PasswordResetTokens] AS T
            WITH (UPDLOCK, HOLDLOCK)

        WHERE
            T.[PasswordResetTokenId] =
                @PasswordResetTokenId;

        IF @UserId IS NULL
        BEGIN
            THROW 51052,
                N'The password reset token was not found.',
                1;
        END;

        /*
            Only an active token needs to be revoked.

            A token already used or revoked is already
            inactive and must not be modified.
        */
        IF @UsedAtUtc IS NULL
           AND @RevokedAtUtc IS NULL
        BEGIN
            UPDATE [Security].[PasswordResetTokens]
            SET
                [RevokedAtUtc] =
                    @OccurredAtUtc
            WHERE
                [PasswordResetTokenId] =
                    @PasswordResetTokenId
                AND [UsedAtUtc] IS NULL
                AND [RevokedAtUtc] IS NULL;

            IF @@ROWCOUNT = 1
            BEGIN
                SET @WasRevoked = 1;

                SET @RevokedAtUtc =
                    @OccurredAtUtc;
            END;
        END;

        SET @IsInactive =
            CONVERT(
                bit,
                CASE
                    WHEN @UsedAtUtc IS NOT NULL
                         OR @RevokedAtUtc IS NOT NULL
                        THEN 1
                    ELSE 0
                END
            );

        /*
            The reset token and its hash are never
            written to Audit.AuditLogs.
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
            [ErrorMessage],
            [OccurredAtUtc]
        )
        VALUES
        (
            @CorrelationId,
            N'Security',
            N'PasswordResetEmailDeliveryFailed',
            N'Users',
            CONVERT(
                nvarchar(100),
                @UserId
            ),
            N'System',
            NULL,
            NULL,
            NULL,
            0,
            CASE
                WHEN @WasRevoked = 1
                THEN
                    N'Password reset email delivery failed. The active reset token was revoked.'
                ELSE
                    N'Password reset email delivery failed. The reset token was already inactive.'
            END,
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            N'The password reset email could not be sent.',
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            [PasswordResetTokenId] =
                @PasswordResetTokenId,

            [UserId] =
                @UserId,

            [RevokedAtUtc] =
                @RevokedAtUtc,

            [WasRevoked] =
                @WasRevoked,

            [IsInactive] =
                @IsInactive;
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