CREATE PROCEDURE [Security].[RegisterSuccessfulLogin]
    @UserId int,
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
    DECLARE @LockoutEndAtUtc datetime2(3);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @EmailAddress = U.[EmailAddress],
            @RoleCode = R.[RoleCode],
            @IsUserActive = U.[IsActive],
            @IsRoleActive = R.[IsActive],
            @LockoutEndAtUtc = U.[LockoutEndAtUtc]
        FROM [Security].[Users] AS U
            WITH (UPDLOCK, HOLDLOCK)

        INNER JOIN [Security].[Roles] AS R
            ON R.[RoleId] = U.[RoleId]

        WHERE U.[UserId] = @UserId;

        IF @EmailAddress IS NULL
        BEGIN
            THROW 51001,
                N'The user was not found.',
                1;
        END;

        IF @IsUserActive = 0
        BEGIN
            THROW 51002,
                N'The user account is inactive.',
                1;
        END;

        IF @IsRoleActive = 0
        BEGIN
            THROW 51003,
                N'The assigned role is inactive.',
                1;
        END;

        IF @LockoutEndAtUtc IS NOT NULL
           AND @LockoutEndAtUtc > @OccurredAtUtc
        BEGIN
            THROW 51004,
                N'The user account is currently locked.',
                1;
        END;

        UPDATE [Security].[Users]
        SET
            [FailedLoginAttempts] = 0,
            [LockoutEndAtUtc] = NULL,
            [LastLoginAtUtc] = @OccurredAtUtc
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
            N'LoginSucceeded',
            N'Users',
            CONVERT(nvarchar(100), @UserId),
            N'User',
            @UserId,
            @EmailAddress,
            @RoleCode,
            1,
            N'User authenticated successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            U.[UserId],
            U.[LastLoginAtUtc],
            U.[FailedLoginAttempts],
            U.[LockoutEndAtUtc]
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