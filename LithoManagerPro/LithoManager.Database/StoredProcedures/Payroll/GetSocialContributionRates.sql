CREATE PROCEDURE [Payroll].[GetSocialContributionRates]
    @AsOfDate date = NULL,
    @IsActive bit = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResolvedAsOfDate date =
        COALESCE(@AsOfDate, CONVERT(date, SYSUTCDATETIME()));

    SELECT
        SCR.[SocialContributionRateId],
        SCR.[SocialContributionTypeId],
        SCT.[ContributionCode],
        SCT.[Name] AS [ContributionName],
        SCT.[InstitutionName],
        SCT.[ContributionGroup],
        SCT.[AppliesToEmployee],
        SCT.[AppliesToEmployer],
        SCT.[UsesMinimumBase],
        SCR.[EmployeeRate],
        SCR.[EmployerRate],
        SCR.[EffectiveFromDate],
        SCR.[EffectiveToDate],
        SCR.[LegalReference],
        SCR.[CreatedAtUtc],
        SCR.[CreatedByUserId],
        SCR.[UpdatedAtUtc],
        SCR.[UpdatedByUserId],
        SCR.[RowVersion]
    FROM [Payroll].[SocialContributionRates] AS SCR
    INNER JOIN [Payroll].[SocialContributionTypes] AS SCT
        ON SCT.[SocialContributionTypeId] =
            SCR.[SocialContributionTypeId]
    WHERE SCR.[EffectiveFromDate] <= @ResolvedAsOfDate
      AND
      (
          SCR.[EffectiveToDate] IS NULL
          OR SCR.[EffectiveToDate] >= @ResolvedAsOfDate
      )
      AND
      (
          @IsActive IS NULL
          OR SCT.[IsActive] = @IsActive
      )
    ORDER BY
        SCT.[ContributionGroup],
        SCT.[InstitutionName],
        SCT.[Name],
        SCR.[EffectiveFromDate] DESC;
END;
GO
