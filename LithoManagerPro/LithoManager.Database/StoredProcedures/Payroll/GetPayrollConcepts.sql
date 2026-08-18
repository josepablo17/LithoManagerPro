CREATE PROCEDURE [Payroll].[GetPayrollConcepts]
    @IsActive bit = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PC.[PayrollConceptId],
        PC.[PayrollConceptCode],
        PC.[Name],
        PC.[Description],
        PC.[ConceptKind],
        PC.[IsSystemConcept],
        PC.[IsTaxableForIncomeTax],
        PC.[IsSubjectToSocialContributions],
        PC.[CountsForAguinaldo],
        PC.[IsActive],
        PC.[CreatedAtUtc],
        PC.[CreatedByUserId],
        PC.[UpdatedAtUtc],
        PC.[UpdatedByUserId],
        PC.[RowVersion]
    FROM [Payroll].[PayrollConcepts] AS PC
    WHERE
        @IsActive IS NULL
        OR PC.[IsActive] = @IsActive
    ORDER BY
        PC.[ConceptKind],
        PC.[Name],
        PC.[PayrollConceptId];
END;
GO
