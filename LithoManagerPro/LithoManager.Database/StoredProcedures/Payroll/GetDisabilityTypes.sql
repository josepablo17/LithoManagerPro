CREATE PROCEDURE [Payroll].[GetDisabilityTypes]
    @IsActive bit = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        DT.[DisabilityTypeId],
        DT.[DisabilityTypeCode],
        DT.[Name],
        DT.[CountsAsSalaryForAguinaldo],
        DT.[RequiresSubsidyTracking],
        DT.[ReducesWorkedDays],
        DT.[IsActive],
        DT.[CreatedAtUtc],
        DT.[CreatedByUserId],
        DT.[UpdatedAtUtc],
        DT.[UpdatedByUserId],
        DT.[RowVersion]
    FROM [Payroll].[DisabilityTypes] AS DT
    WHERE
        @IsActive IS NULL
        OR DT.[IsActive] = @IsActive
    ORDER BY
        DT.[Name],
        DT.[DisabilityTypeId];
END;
GO
