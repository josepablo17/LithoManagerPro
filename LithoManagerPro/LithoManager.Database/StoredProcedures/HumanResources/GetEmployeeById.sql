CREATE PROCEDURE [HumanResources].[GetEmployeeById]
    @EmployeeId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @EmployeeId IS NULL
       OR @EmployeeId <= 0
    BEGIN
        RETURN;
    END;

    SELECT
        E.[EmployeeId],
        E.[UserId],
        U.[EmailAddress],
        E.[DepartmentId],
        D.[DepartmentCode],
        D.[Name] AS [DepartmentName],
        D.[IsActive] AS [IsDepartmentActive],
        E.[IdentificationNumber],
        E.[FirstName],
        E.[LastName],
        E.[PhoneNumber],
        E.[BirthDate],
        E.[HireDate],
        E.[TerminationDate],
        E.[JobTitle],
        E.[BaseSalary],
        E.[ProfileImagePath],
        E.[IsActive],
        E.[CreatedAtUtc],
        E.[CreatedByUserId],
        E.[UpdatedAtUtc],
        E.[UpdatedByUserId],
        E.[RowVersion]
    FROM [HumanResources].[Employees] AS E
    LEFT JOIN [Security].[Users] AS U
        ON U.[UserId] = E.[UserId]
    INNER JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]
    WHERE E.[EmployeeId] = @EmployeeId;
END;
GO
