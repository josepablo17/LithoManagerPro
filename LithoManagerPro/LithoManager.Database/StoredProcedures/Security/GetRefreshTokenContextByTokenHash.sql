CREATE PROCEDURE
    [Security].[GetRefreshTokenContextByTokenHash]
    @TokenHash varbinary(32)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OccurredAtUtc datetime2(3) =
        SYSUTCDATETIME();

    IF @TokenHash IS NULL
       OR DATALENGTH(@TokenHash) <> 32
    BEGIN
        THROW 51320,
            N'Token hash must contain exactly 32 bytes.',
            1;
    END;

    /*
        This procedure only returns a refresh context
        when the refresh token is currently usable and
        the account is still eligible to sign in.

        No plaintext refresh token, token hash or cookie
        value is returned by SQL Server.
    */
    SELECT
        T.[RefreshTokenId],
        T.[UserId],
        T.[TokenFamilyId],
        T.[TokenVersion] AS [RefreshTokenVersion],
        T.[ExpiresAtUtc],
        T.[CreatedAtUtc],
        T.[LastUsedAtUtc],

        U.[EmailAddress],
        U.[TokenVersion],
        U.[IsEmailConfirmed],
        U.[IsActive],
        U.[RequiresPasswordChange],

        R.[RoleId],
        R.[RoleCode],
        R.[DisplayName] AS [RoleDisplayName],
        R.[IsActive] AS [IsRoleActive],

        E.[EmployeeId],
        E.[FirstName],
        E.[LastName],
        E.[JobTitle],
        E.[ProfileImagePath],
        E.[IsActive] AS [IsEmployeeActive],

        D.[DepartmentId],
        D.[DepartmentCode],
        D.[Name] AS [DepartmentName],
        D.[IsActive] AS [IsDepartmentActive]

    FROM [Security].[RefreshTokens] AS T

    INNER JOIN [Security].[Users] AS U
        ON U.[UserId] = T.[UserId]

    INNER JOIN [Security].[Roles] AS R
        ON R.[RoleId] = U.[RoleId]

    LEFT JOIN [HumanResources].[Employees] AS E
        ON E.[UserId] = U.[UserId]

    LEFT JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]

    WHERE
        T.[TokenHash] = @TokenHash
        AND T.[ReplacedAtUtc] IS NULL
        AND T.[RevokedAtUtc] IS NULL
        AND T.[ExpiresAtUtc] > @OccurredAtUtc
        AND T.[TokenVersion] = U.[TokenVersion]

        AND U.[IsActive] = 1
        AND U.[IsEmailConfirmed] = 1

        AND R.[IsActive] = 1

        AND
        (
            E.[EmployeeId] IS NULL
            OR E.[IsActive] = 1
        )

        AND
        (
            D.[DepartmentId] IS NULL
            OR D.[IsActive] = 1
        );
END;
GO
