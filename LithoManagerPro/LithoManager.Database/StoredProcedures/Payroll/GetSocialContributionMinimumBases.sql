CREATE PROCEDURE [Payroll].[GetSocialContributionMinimumBases]
    @AsOfDate date = NULL,
    @IsActive bit = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResolvedAsOfDate date =
        COALESCE(@AsOfDate, CONVERT(date, SYSUTCDATETIME()));

    SELECT
        SCMB.[SocialContributionMinimumBaseId],
        SCMB.[SocialContributionTypeId],
        SCT.[ContributionCode],
        SCT.[Name] AS [ContributionName],
        SCT.[InstitutionName],
        SCT.[ContributionGroup],
        SCMB.[MinimumBaseAmount],
        SCMB.[EffectiveFromDate],
        SCMB.[EffectiveToDate],
        SCMB.[LegalReference],
        SCMB.[CreatedAtUtc],
        SCMB.[CreatedByUserId],
        SCMB.[UpdatedAtUtc],
        SCMB.[UpdatedByUserId],
        SCMB.[RowVersion]
    FROM [Payroll].[SocialContributionMinimumBases] AS SCMB
    INNER JOIN [Payroll].[SocialContributionTypes] AS SCT
        ON SCT.[SocialContributionTypeId] =
            SCMB.[SocialContributionTypeId]
    WHERE SCMB.[EffectiveFromDate] <= @ResolvedAsOfDate
      AND
      (
          SCMB.[EffectiveToDate] IS NULL
          OR SCMB.[EffectiveToDate] >= @ResolvedAsOfDate
      )
      AND
      (
          @IsActive IS NULL
          OR SCT.[IsActive] = @IsActive
      )
    ORDER BY
        SCT.[ContributionGroup],
        SCT.[Name],
        SCMB.[EffectiveFromDate] DESC;
END;
GO
