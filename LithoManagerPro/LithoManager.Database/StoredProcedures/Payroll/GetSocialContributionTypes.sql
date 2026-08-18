CREATE PROCEDURE [Payroll].[GetSocialContributionTypes]
    @IsActive bit = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SCT.[SocialContributionTypeId],
        SCT.[ContributionCode],
        SCT.[Name],
        SCT.[InstitutionName],
        SCT.[ContributionGroup],
        SCT.[AppliesToEmployee],
        SCT.[AppliesToEmployer],
        SCT.[UsesMinimumBase],
        SCT.[IsActive],
        SCT.[CreatedAtUtc],
        SCT.[CreatedByUserId],
        SCT.[UpdatedAtUtc],
        SCT.[UpdatedByUserId],
        SCT.[RowVersion]
    FROM [Payroll].[SocialContributionTypes] AS SCT
    WHERE
        @IsActive IS NULL
        OR SCT.[IsActive] = @IsActive
    ORDER BY
        SCT.[ContributionGroup],
        SCT.[InstitutionName],
        SCT.[Name],
        SCT.[SocialContributionTypeId];
END;
GO
