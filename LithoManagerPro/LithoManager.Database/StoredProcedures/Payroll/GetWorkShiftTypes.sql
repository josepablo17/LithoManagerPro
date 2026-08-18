CREATE PROCEDURE [Payroll].[GetWorkShiftTypes]
    @AsOfDate date = NULL,
    @IsActive bit = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResolvedAsOfDate date =
        COALESCE(@AsOfDate, CONVERT(date, SYSUTCDATETIME()));

    SELECT
        WST.[WorkShiftTypeId],
        WST.[WorkShiftTypeCode],
        WST.[Name],
        WST.[MaxOrdinaryHoursPerDay],
        WST.[MaxOrdinaryHoursPerWeek],
        WST.[MaxTotalHoursPerDay],
        WST.[EffectiveFromDate],
        WST.[EffectiveToDate],
        WST.[IsActive],
        WST.[CreatedAtUtc],
        WST.[CreatedByUserId],
        WST.[UpdatedAtUtc],
        WST.[UpdatedByUserId],
        WST.[RowVersion]
    FROM [Payroll].[WorkShiftTypes] AS WST
    WHERE WST.[EffectiveFromDate] <= @ResolvedAsOfDate
      AND
      (
          WST.[EffectiveToDate] IS NULL
          OR WST.[EffectiveToDate] >= @ResolvedAsOfDate
      )
      AND
      (
          @IsActive IS NULL
          OR WST.[IsActive] = @IsActive
      )
    ORDER BY
        WST.[Name],
        WST.[WorkShiftTypeId];
END;
GO
