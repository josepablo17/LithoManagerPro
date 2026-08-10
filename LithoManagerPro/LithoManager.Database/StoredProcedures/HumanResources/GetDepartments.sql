CREATE PROCEDURE [HumanResources].[GetDepartments]
    @SearchTerm nvarchar(4000) = NULL,
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
       AND LEN(@NormalizedSearchTerm) > 100
    BEGIN
        THROW 52021,
            N'SearchTerm cannot exceed 100 characters.',
            1;
    END;

    SELECT
        D.[DepartmentId],
        D.[DepartmentCode],
        D.[Name],
        D.[Description],
        D.[IsActive],
        D.[CreatedAtUtc],
        D.[CreatedByUserId],
        D.[UpdatedAtUtc],
        D.[UpdatedByUserId],
        D.[RowVersion]
    FROM [HumanResources].[Departments] AS D
    WHERE
        (
            @IsActive IS NULL
            OR D.[IsActive] = @IsActive
        )
        AND
        (
            @NormalizedSearchTerm IS NULL
            OR D.[DepartmentCode] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR D.[Name] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
        )
    ORDER BY
        D.[Name],
        D.[DepartmentCode],
        D.[DepartmentId];
END;
GO
