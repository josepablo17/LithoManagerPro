CREATE PROCEDURE [Payroll].[GetPayrollRunEmployees]
    @PayrollRunId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        PRE.[PayrollRunEmployeeId],
        PRE.[PayrollRunId],
        PRE.[EmployeeId],
        PRE.[IdentificationNumber],
        PRE.[EmployeeName],
        PRE.[DepartmentName],
        PRE.[JobTitle],
        PRE.[MonthlyBaseSalary],
        PRE.[PaidDays],
        PRE.[GrossAmount],
        PRE.[EmployeeContributionsAmount],
        PRE.[IncomeTaxAmount],
        PRE.[NetAmount],
        PRE.[EmployerContributionsAmount]
    FROM [Payroll].[PayrollRunEmployees] AS PRE
    WHERE PRE.[PayrollRunId] = @PayrollRunId
    ORDER BY PRE.[EmployeeName];
END;
GO
