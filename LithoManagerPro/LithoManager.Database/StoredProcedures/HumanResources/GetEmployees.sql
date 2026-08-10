CREATE PROCEDURE [HumanResources].[GetEmployees]
    @SearchTerm nvarchar(4000) = NULL,
    @DepartmentId int = NULL,
    @IsActive bit = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedSearchTerm nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@SearchTerm)),
            N''
        );

    IF @NormalizedSearchTerm IS NOT NULL
       AND LEN(@NormalizedSearchTerm) > 150
    BEGIN
        THROW 52131,
            N'SearchTerm cannot exceed 150 characters.',
            1;
    END;

    IF @DepartmentId IS NOT NULL
       AND @DepartmentId <= 0
    BEGIN
        THROW 52132,
            N'DepartmentId must be greater than zero.',
            1;
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
    WHERE
        (
            @IsActive IS NULL
            OR E.[IsActive] = @IsActive
        )
        AND
        (
            @DepartmentId IS NULL
            OR E.[DepartmentId] = @DepartmentId
        )
        AND
        (
            @NormalizedSearchTerm IS NULL
            OR E.[IdentificationNumber] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR E.[FirstName] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR E.[LastName] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR E.[JobTitle] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR D.[DepartmentCode] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR D.[Name] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR U.[EmailAddress] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
        )
    ORDER BY
        E.[LastName],
        E.[FirstName],
        E.[EmployeeId];
END;
GO
