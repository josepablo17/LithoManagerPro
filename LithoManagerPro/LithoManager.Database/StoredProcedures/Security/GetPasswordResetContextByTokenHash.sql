CREATE PROCEDURE
    [Security].[GetPasswordResetContextByTokenHash]
    @TokenHash varbinary(32)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OccurredAtUtc datetime2(3) =
        SYSUTCDATETIME();

    IF @TokenHash IS NULL
       OR DATALENGTH(@TokenHash) <> 32
    BEGIN
        THROW 51060,
            N'Token hash must contain exactly 32 bytes.',
            1;
    END;

    /*
        This procedure only returns a reset context
        when the token is currently usable and the
        associated account is still eligible.

        No plaintext reset token is ever stored or
        returned by SQL Server.
    */
    SELECT
        T.[PasswordResetTokenId],
        T.[UserId],
        U.[PasswordHash],
        T.[ExpiresAtUtc]

    FROM [Security].[PasswordResetTokens] AS T

    INNER JOIN [Security].[Users] AS U
        ON U.[UserId] = T.[UserId]

    INNER JOIN [Security].[Roles] AS R
        ON R.[RoleId] = U.[RoleId]

    LEFT JOIN [HumanResources].[Employees] AS E
        ON E.[UserId] = U.[UserId]

    WHERE
        T.[TokenHash] = @TokenHash
        AND T.[UsedAtUtc] IS NULL
        AND T.[RevokedAtUtc] IS NULL
        AND T.[ExpiresAtUtc] > @OccurredAtUtc

        AND U.[IsActive] = 1
        AND U.[IsEmailConfirmed] = 1

        AND R.[IsActive] = 1

        AND
        (
            E.[EmployeeId] IS NULL
            OR E.[IsActive] = 1
        );
END;
GO