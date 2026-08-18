CREATE PROCEDURE [Payroll].[GetIncomeTaxBrackets]
    @TaxYear smallint,
    @Periodicity nvarchar(20) = N'Monthly',
    @AsOfDate date = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @TaxYear IS NULL
       OR @TaxYear < 2000
       OR @TaxYear > 2100
    BEGIN
        THROW 56001,
            N'TaxYear must be between 2000 and 2100.',
            1;
    END;

    IF @Periodicity NOT IN (N'Monthly', N'Annual')
    BEGIN
        THROW 56002,
            N'Periodicity must be Monthly or Annual.',
            1;
    END;

    DECLARE @ResolvedAsOfDate date =
        COALESCE(@AsOfDate, CONVERT(date, SYSUTCDATETIME()));

    SELECT
        ITB.[IncomeTaxBracketId],
        ITB.[TaxYear],
        ITB.[Periodicity],
        ITB.[LowerBoundAmount],
        ITB.[UpperBoundAmount],
        ITB.[TaxRate],
        ITB.[EffectiveFromDate],
        ITB.[EffectiveToDate],
        ITB.[LegalReference],
        ITB.[CreatedAtUtc],
        ITB.[CreatedByUserId],
        ITB.[UpdatedAtUtc],
        ITB.[UpdatedByUserId],
        ITB.[RowVersion]
    FROM [Payroll].[IncomeTaxBrackets] AS ITB
    WHERE ITB.[TaxYear] = @TaxYear
      AND ITB.[Periodicity] = @Periodicity
      AND ITB.[EffectiveFromDate] <= @ResolvedAsOfDate
      AND
      (
          ITB.[EffectiveToDate] IS NULL
          OR ITB.[EffectiveToDate] >= @ResolvedAsOfDate
      )
    ORDER BY
        ITB.[LowerBoundAmount],
        ITB.[IncomeTaxBracketId];
END;
GO
