CREATE PROCEDURE [Payroll].[GetIncomeTaxCredits]
    @TaxYear smallint,
    @Periodicity nvarchar(20) = N'Monthly',
    @AsOfDate date = NULL,
    @IsActive bit = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @TaxYear IS NULL
       OR @TaxYear < 2000
       OR @TaxYear > 2100
    BEGIN
        THROW 56011,
            N'TaxYear must be between 2000 and 2100.',
            1;
    END;

    IF @Periodicity NOT IN (N'Monthly', N'Annual')
    BEGIN
        THROW 56012,
            N'Periodicity must be Monthly or Annual.',
            1;
    END;

    DECLARE @ResolvedAsOfDate date =
        COALESCE(@AsOfDate, CONVERT(date, SYSUTCDATETIME()));

    SELECT
        ITC.[IncomeTaxCreditId],
        ITC.[CreditCode],
        ITC.[Name],
        ITC.[TaxYear],
        ITC.[Periodicity],
        ITC.[CreditAmount],
        ITC.[EffectiveFromDate],
        ITC.[EffectiveToDate],
        ITC.[LegalReference],
        ITC.[IsActive],
        ITC.[CreatedAtUtc],
        ITC.[CreatedByUserId],
        ITC.[UpdatedAtUtc],
        ITC.[UpdatedByUserId],
        ITC.[RowVersion]
    FROM [Payroll].[IncomeTaxCredits] AS ITC
    WHERE ITC.[TaxYear] = @TaxYear
      AND ITC.[Periodicity] = @Periodicity
      AND ITC.[EffectiveFromDate] <= @ResolvedAsOfDate
      AND
      (
          ITC.[EffectiveToDate] IS NULL
          OR ITC.[EffectiveToDate] >= @ResolvedAsOfDate
      )
      AND
      (
          @IsActive IS NULL
          OR ITC.[IsActive] = @IsActive
      )
    ORDER BY
        ITC.[Name],
        ITC.[IncomeTaxCreditId];
END;
GO
