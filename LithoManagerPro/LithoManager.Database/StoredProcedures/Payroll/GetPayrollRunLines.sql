CREATE PROCEDURE [Payroll].[GetPayrollRunLines]
    @PayrollRunId int,
    @EmployeeId int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PRL.[PayrollRunLineId],
        PRE.[PayrollRunEmployeeId],
        PRE.[EmployeeId],
        PRL.[PayrollConceptId],
        PC.[PayrollConceptCode],
        PC.[ConceptKind],
        PRL.[ReferenceCode],
        PRL.[Description],
        PRL.[Quantity],
        PRL.[Rate],
        PRL.[BaseAmount],
        PRL.[Amount],
        PRL.[SortOrder]
    FROM [Payroll].[PayrollRunLines] AS PRL
    INNER JOIN [Payroll].[PayrollRunEmployees] AS PRE
        ON PRE.[PayrollRunEmployeeId] = PRL.[PayrollRunEmployeeId]
    INNER JOIN [Payroll].[PayrollConcepts] AS PC
        ON PC.[PayrollConceptId] = PRL.[PayrollConceptId]
    WHERE PRE.[PayrollRunId] = @PayrollRunId
      AND (@EmployeeId IS NULL OR PRE.[EmployeeId] = @EmployeeId)
    ORDER BY
        PRE.[EmployeeId],
        PRL.[SortOrder];
END;
GO
