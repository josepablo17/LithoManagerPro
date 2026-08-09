CREATE PROCEDURE [Security].[GetUserTokenValidationById]
    @UserId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @UserId <= 0
    BEGIN
        RETURN;
    END;

    SELECT
        U.[UserId],
        U.[TokenVersion],
        U.[IsActive] AS [IsUserActive],
        R.[IsActive] AS [IsRoleActive],
        E.[EmployeeId],
        E.[IsActive] AS [IsEmployeeActive]

    FROM [Security].[Users] AS U

    INNER JOIN [Security].[Roles] AS R
        ON R.[RoleId] = U.[RoleId]

    LEFT JOIN [HumanResources].[Employees] AS E
        ON E.[UserId] = U.[UserId]

    WHERE U.[UserId] = @UserId;
END;
GO
