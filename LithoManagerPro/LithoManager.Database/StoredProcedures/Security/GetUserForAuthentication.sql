CREATE PROCEDURE [Security].[GetUserForAuthentication]
    @EmailAddress nvarchar(254)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedEmailAddress nvarchar(254);

    SET @NormalizedEmailAddress =
        LTRIM(RTRIM(@EmailAddress));

    IF @NormalizedEmailAddress IS NULL
       OR @NormalizedEmailAddress = N''
    BEGIN
        RETURN;
    END;

    SELECT
        U.[UserId],
        U.[EmailAddress],
        U.[PasswordHash],
        U.[IsEmailConfirmed],
        U.[IsActive],
        U.[RequiresPasswordChange],
        U.[TemporaryPasswordExpiresAtUtc],
        U.[PasswordChangedAtUtc],
        U.[FailedLoginAttempts],
        U.[LockoutEndAtUtc],
        U.[LastLoginAtUtc],

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
        D.[Name] AS [DepartmentName]

    FROM [Security].[Users] AS U

    INNER JOIN [Security].[Roles] AS R
        ON R.[RoleId] = U.[RoleId]

    LEFT JOIN [HumanResources].[Employees] AS E
        ON E.[UserId] = U.[UserId]

    LEFT JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]

    WHERE U.[EmailAddress] = @NormalizedEmailAddress;
END;
GO