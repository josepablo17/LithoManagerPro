CREATE PROCEDURE [Payroll].[GetAguinaldoRules]
    @AsOfDate date = NULL,
    @IsActive bit = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResolvedAsOfDate date =
        COALESCE(@AsOfDate, CONVERT(date, SYSUTCDATETIME()));

    SELECT
        AR.[AguinaldoRuleId],
        AR.[AguinaldoRuleCode],
        AR.[Name],
        AR.[CalculationStartMonth],
        AR.[CalculationStartDay],
        AR.[CalculationEndMonth],
        AR.[CalculationEndDay],
        AR.[Divisor],
        AR.[PaymentDueMonth],
        AR.[PaymentDueDay],
        AR.[IncludesOrdinarySalary],
        AR.[IncludesOvertime],
        AR.[IncludesSalaryInKind],
        AR.[ExcludesCommonIllnessSubsidy],
        AR.[IncludesMaternitySubsidy],
        AR.[EffectiveFromDate],
        AR.[EffectiveToDate],
        AR.[IsActive],
        AR.[CreatedAtUtc],
        AR.[CreatedByUserId],
        AR.[UpdatedAtUtc],
        AR.[UpdatedByUserId],
        AR.[RowVersion]
    FROM [Payroll].[AguinaldoRules] AS AR
    WHERE AR.[EffectiveFromDate] <= @ResolvedAsOfDate
      AND
      (
          AR.[EffectiveToDate] IS NULL
          OR AR.[EffectiveToDate] >= @ResolvedAsOfDate
      )
      AND
      (
          @IsActive IS NULL
          OR AR.[IsActive] = @IsActive
      )
    ORDER BY
        AR.[Name],
        AR.[AguinaldoRuleId];
END;
GO
