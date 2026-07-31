CREATE PROCEDURE [Security].[ChangeTemporaryPassword]
    @UserId int,
    @NewPasswordHash nvarchar(500),
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

    DECLARE @EmailAddress nvarchar(254);
    DECLARE @RoleCode nvarchar(50);
    DECLARE @IsUserActive bit;
    DECLARE @IsRoleActive bit;
    DECLARE @RequiresPasswordChange bit;
    DECLARE @TemporaryPasswordExpiresAtUtc datetime2(3);
    DECLARE @LockoutEndAtUtc datetime2(3);

    IF @UserId <= 0
    BEGIN
        THROW 51101,
            N'The UserId must be greater than zero.',
            1;
    END;

    IF NULLIF(
        LTRIM(RTRIM(@NewPasswordHash)),
        N'') IS NULL
    BEGIN
        THROW 51102,
            N'The new password hash is required.',
            1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @EmailAddress = U.[EmailAddress],
            @RoleCode = R.[RoleCode],
            @IsUserActive = U.[IsActive],
            @IsRoleActive = R.[IsActive],
            @RequiresPasswordChange =
                U.[RequiresPasswordChange],
            @TemporaryPasswordExpiresAtUtc =
                U.[TemporaryPasswordExpiresAtUtc],
            @LockoutEndAtUtc =
                U.[LockoutEndAtUtc]
        FROM [Security].[Users] AS U
            WITH (UPDLOCK, HOLDLOCK)

        INNER JOIN [Security].[Roles] AS R
            ON R.[RoleId] = U.[RoleId]

        WHERE U.[UserId] = @UserId;

        IF @EmailAddress IS NULL
        BEGIN
            THROW 51103,
                N'The user was not found.',
                1;
        END;

        IF @IsUserActive = 0
        BEGIN
            THROW 51104,
                N'The user account is inactive.',
                1;
        END;

        IF @IsRoleActive = 0
        BEGIN
            THROW 51105,
                N'The assigned role is inactive.',
                1;
        END;

        IF @LockoutEndAtUtc IS NOT NULL
           AND @LockoutEndAtUtc > @OccurredAtUtc
        BEGIN
            THROW 51106,
                N'The user account is currently locked.',
                1;
        END;

        IF @RequiresPasswordChange = 0
        BEGIN
            THROW 51107,
                N'The user does not require a temporary password change.',
                1;
        END;

        IF @TemporaryPasswordExpiresAtUtc IS NULL
           OR @TemporaryPasswordExpiresAtUtc
                <= @OccurredAtUtc
        BEGIN
            THROW 51108,
                N'The temporary password has expired.',
                1;
        END;

        UPDATE [Security].[Users]
        SET
            [PasswordHash] = @NewPasswordHash,
            [RequiresPasswordChange] = 0,
            [TemporaryPasswordExpiresAtUtc] = NULL,
            [PasswordChangedAtUtc] = @OccurredAtUtc,
            [FailedLoginAttempts] = 0,
            [LockoutEndAtUtc] = NULL,
            [UpdatedAtUtc] = @OccurredAtUtc,
            [UpdatedByUserId] = @UserId
        WHERE [UserId] = @UserId;

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
            @ResolvedCorrelationId,
            N'Security',
            N'TemporaryPasswordChanged',
            N'Users',
            CONVERT(nvarchar(100), @UserId),
            N'User',
            @UserId,
            @EmailAddress,
            @RoleCode,
            1,
            N'User replaced the temporary password successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            U.[UserId],
            U.[PasswordChangedAtUtc],
            U.[RequiresPasswordChange]
        FROM [Security].[Users] AS U
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