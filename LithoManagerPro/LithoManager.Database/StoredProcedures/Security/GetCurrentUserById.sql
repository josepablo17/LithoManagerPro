CREATE PROCEDURE [Security].[GetCurrentUserById]
    @UserId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @UserId IS NULL
       OR @UserId <= 0
    BEGIN
        RETURN;
    END;

    SELECT
        U.[UserId],
        U.[EmailAddress],
        U.[IsEmailConfirmed],
        U.[IsActive],
        U.[RequiresPasswordChange],

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

    FROM [Security].[Users] AS U

    INNER JOIN [Security].[Roles] AS R
        ON R.[RoleId] = U.[RoleId]

    LEFT JOIN [HumanResources].[Employees] AS E
        ON E.[UserId] = U.[UserId]

    LEFT JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]

    WHERE U.[UserId] = @UserId;
END;
GO