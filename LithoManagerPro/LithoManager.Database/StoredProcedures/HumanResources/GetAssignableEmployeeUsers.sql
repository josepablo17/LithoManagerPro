CREATE PROCEDURE [HumanResources].[GetAssignableEmployeeUsers]
    @EmployeeId int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @EmployeeId IS NOT NULL
       AND @EmployeeId <= 0
    BEGIN
        THROW 52133,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    SELECT
        U.[UserId],
        U.[EmailAddress],
        R.[RoleId],
        R.[RoleCode],
        R.[DisplayName] AS [RoleName],
        AssignedEmployee.[EmployeeId] AS [AssignedEmployeeId],
        AssignedEmployee.[FirstName]
            AS [AssignedEmployeeFirstName],
        AssignedEmployee.[LastName]
            AS [AssignedEmployeeLastName]
    FROM [Security].[Users] AS U
    INNER JOIN [Security].[Roles] AS R
        ON R.[RoleId] = U.[RoleId]
    LEFT JOIN [HumanResources].[Employees] AS AssignedEmployee
        ON AssignedEmployee.[UserId] = U.[UserId]
    WHERE
        U.[IsActive] = 1
        AND R.[IsActive] = 1
        AND
        (
            AssignedEmployee.[EmployeeId] IS NULL
            OR AssignedEmployee.[EmployeeId] = @EmployeeId
        )
    ORDER BY
        U.[EmailAddress],
        U.[UserId];
END;
GO
