CREATE PROCEDURE [Payroll].[GetOvertimeRules]
    @AsOfDate date = NULL,
    @IsActive bit = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResolvedAsOfDate date =
        COALESCE(@AsOfDate, CONVERT(date, SYSUTCDATETIME()));

    SELECT
        OTR.[OvertimeRuleId],
        OTR.[OvertimeRuleCode],
        OTR.[Name],
        OTR.[HourMultiplier],
        OTR.[CountsForAguinaldo],
        OTR.[EffectiveFromDate],
        OTR.[EffectiveToDate],
        OTR.[IsActive],
        OTR.[CreatedAtUtc],
        OTR.[CreatedByUserId],
        OTR.[UpdatedAtUtc],
        OTR.[UpdatedByUserId],
        OTR.[RowVersion]
    FROM [Payroll].[OvertimeRules] AS OTR
    WHERE OTR.[EffectiveFromDate] <= @ResolvedAsOfDate
      AND
      (
          OTR.[EffectiveToDate] IS NULL
          OR OTR.[EffectiveToDate] >= @ResolvedAsOfDate
      )
      AND
      (
          @IsActive IS NULL
          OR OTR.[IsActive] = @IsActive
      )
    ORDER BY
        OTR.[Name],
        OTR.[OvertimeRuleId];
END;
GO
