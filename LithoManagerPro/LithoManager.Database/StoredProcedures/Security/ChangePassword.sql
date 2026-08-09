CREATE PROCEDURE [Security].[ChangePassword]
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
        COALESCE(
            @CorrelationId,
            NEWID()
        );

    IF @UserId <= 0
    BEGIN
        THROW 51201,
            N'The UserId must be greater than zero.',
            1;
    END;

    IF NULLIF(
        LTRIM(RTRIM(@NewPasswordHash)),
        N'') IS NULL
    BEGIN
        THROW 51202,
            N'The new password hash is required.',
            1;
    END;

    DECLARE @EmailAddress nvarchar(254);
    DECLARE @RoleCode nvarchar(50);

    DECLARE @IsUserActive bit;
    DECLARE @IsRoleActive bit;

    DECLARE @RequiresPasswordChange bit;
    DECLARE @LockoutEndAtUtc datetime2(3);

    DECLARE @EmployeeId int;
    DECLARE @IsEmployeeActive bit;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @EmailAddress =
                U.[EmailAddress],

            @RoleCode =
                R.[RoleCode],

            @IsUserActive =
                U.[IsActive],

            @IsRoleActive =
                R.[IsActive],

            @RequiresPasswordChange =
                U.[RequiresPasswordChange],

            @LockoutEndAtUtc =
                U.[LockoutEndAtUtc],

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

        WHERE U.[UserId] = @UserId;

        IF @EmailAddress IS NULL
        BEGIN
            THROW 51203,
                N'The user was not found.',
                1;
        END;

        IF @IsUserActive = 0
        BEGIN
            THROW 51204,
                N'The user account is inactive.',
                1;
        END;

        IF @IsRoleActive = 0
        BEGIN
            THROW 51205,
                N'The assigned role is inactive.',
                1;
        END;

        IF @EmployeeId IS NOT NULL
           AND @IsEmployeeActive <> 1
        BEGIN
            THROW 51206,
                N'The employee record is inactive.',
                1;
        END;

        IF @LockoutEndAtUtc IS NOT NULL
           AND @LockoutEndAtUtc > @OccurredAtUtc
        BEGIN
            THROW 51207,
                N'The user account is currently locked.',
                1;
        END;

        /*
            A user with a temporary password must use
            Security.ChangeTemporaryPassword instead.
        */

        IF @RequiresPasswordChange = 1
        BEGIN
            THROW 51208,
                N'The user must complete the temporary password change flow.',
                1;
        END;

        UPDATE [Security].[Users]
        SET
            [PasswordHash] =
                @NewPasswordHash,

            [PasswordChangedAtUtc] =
                @OccurredAtUtc,

            [TokenVersion] =
                [TokenVersion] + 1,

            [FailedLoginAttempts] =
                0,

            [LockoutEndAtUtc] =
                NULL,

            [UpdatedAtUtc] =
                @OccurredAtUtc,

            [UpdatedByUserId] =
                @UserId

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
            N'PasswordChanged',
            N'Users',
            CONVERT(
                nvarchar(100),
                @UserId
            ),
            N'User',
            @UserId,
            @EmailAddress,
            @RoleCode,
            1,
            N'The user changed the account password successfully.',
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
            U.[RequiresPasswordChange],
            U.[TokenVersion]

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
