CREATE PROCEDURE [HumanResources].[GetDepartmentById]
    @DepartmentId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @DepartmentId IS NULL
       OR @DepartmentId <= 0
    BEGIN
        RETURN;
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
    WHERE D.[DepartmentId] = @DepartmentId;
END;
GO
