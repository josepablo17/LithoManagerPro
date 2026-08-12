CREATE PROCEDURE [Security].[CreateRefreshToken]
    @UserId int,
    @TokenHash varbinary(32),
    @TokenFamilyId uniqueidentifier,
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

    DECLARE @RefreshTokenId int = NULL;
    DECLARE @EmailAddress nvarchar(254) = NULL;
    DECLARE @RoleCode nvarchar(50) = NULL;
    DECLARE @IsEmailConfirmed bit = NULL;
    DECLARE @IsUserActive bit = NULL;
    DECLARE @IsRoleActive bit = NULL;
    DECLARE @TokenVersion int = NULL;
    DECLARE @EmployeeId int = NULL;
    DECLARE @IsEmployeeActive bit = NULL;
    DECLARE @DepartmentId int = NULL;
    DECLARE @IsDepartmentActive bit = NULL;

    IF @UserId IS NULL
       OR @UserId <= 0
    BEGIN
        THROW 51301,
            N'UserId is required.',
            1;
    END;

    IF @TokenHash IS NULL
       OR DATALENGTH(@TokenHash) <> 32
    BEGIN
        THROW 51302,
            N'Token hash must contain exactly 32 bytes.',
            1;
    END;

    IF @TokenFamilyId IS NULL
       OR @TokenFamilyId =
            CONVERT(
                uniqueidentifier,
                '00000000-0000-0000-0000-000000000000'
            )
    BEGIN
        THROW 51303,
            N'TokenFamilyId is required.',
            1;
    END;

    IF @ExpiresAtUtc IS NULL
       OR @ExpiresAtUtc <= @OccurredAtUtc
    BEGIN
        THROW 51304,
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
        THROW 51305,
            N'CorrelationId is required.',
            1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @EmailAddress =
                U.[EmailAddress],

            @RoleCode =
                R.[RoleCode],

            @IsEmailConfirmed =
                U.[IsEmailConfirmed],

            @IsUserActive =
                U.[IsActive],

            @IsRoleActive =
                R.[IsActive],

            @TokenVersion =
                U.[TokenVersion],

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

        IF @EmailAddress IS NULL
        BEGIN
            THROW 51306,
                N'The user was not found.',
                1;
        END;

        IF @IsUserActive = 0
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
            THROW 51307,
                N'The user account is not eligible for a refresh session.',
                1;
        END;

        /*
            The product rule is one active refresh session per user.
            A new login closes any previous active session before
            the new token is inserted.
        */
        UPDATE [Security].[RefreshTokens]
        SET
            [RevokedAtUtc] =
                @OccurredAtUtc,

            [RevokedReason] =
                N'NewLogin'

        WHERE
            [UserId] = @UserId
            AND [ReplacedAtUtc] IS NULL
            AND [RevokedAtUtc] IS NULL;

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
            @TokenHash,
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

        SET @RefreshTokenId =
            CONVERT(int, SCOPE_IDENTITY());

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
            N'RefreshTokenCreated',
            N'RefreshTokens',
            CONVERT(nvarchar(100), @RefreshTokenId),
            N'User',
            @UserId,
            @EmailAddress,
            @RoleCode,
            1,
            N'Refresh session created successfully.',
            @ClientIpAddress,
            @UserAgent,
            N'POST',
            @RequestPath,
            @OccurredAtUtc
        );

        COMMIT TRANSACTION;

        SELECT
            [RefreshTokenId] =
                @RefreshTokenId,

            [UserId] =
                @UserId,

            [TokenFamilyId] =
                @TokenFamilyId,

            [TokenVersion] =
                @TokenVersion,

            [ExpiresAtUtc] =
                @ExpiresAtUtc,

            [CreatedAtUtc] =
                @OccurredAtUtc;
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
