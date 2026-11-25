CREATE PROCEDURE [Payroll].[GetPayrollRunById]
    @PayrollRunId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PR.[PayrollRunId],
        PR.[RunType],
        PR.[PeriodYear],
        PR.[PeriodMonth],
        PR.[PeriodStartDate],
        PR.[PeriodEndDate],
        PR.[PaymentDate],
        PR.[Status],
        PR.[EmployeeCount],
        PR.[TotalGrossAmount],
        PR.[TotalEmployeeContributionsAmount],
        PR.[TotalIncomeTaxAmount],
        PR.[TotalNetAmount],
        PR.[TotalEmployerContributionsAmount],
        PR.[CalculatedAtUtc],
        PR.[CalculatedByUserId],
        CU.[EmailAddress] AS [CalculatedByEmailAddress],
        PR.[ClosedAtUtc],
        PR.[ClosedByUserId],
        XU.[EmailAddress] AS [ClosedByEmailAddress],
        PR.[CreatedAtUtc],
        PR.[CreatedByUserId],
        PR.[UpdatedAtUtc],
        PR.[UpdatedByUserId],
        PR.[RowVersion]
    FROM [Payroll].[PayrollRuns] AS PR
    LEFT JOIN [Security].[Users] AS CU
        ON CU.[UserId] = PR.[CalculatedByUserId]
    LEFT JOIN [Security].[Users] AS XU
        ON XU.[UserId] = PR.[ClosedByUserId]
    WHERE PR.[PayrollRunId] = @PayrollRunId;
END;
GO
