SET NOCOUNT ON;
SET XACT_ABORT ON;

INSERT INTO [HumanResources].[EmployeeSalaryHistory]
(
    [EmployeeId],
    [BaseSalary],
    [EffectiveFromDate],
    [CreatedByUserId]
)
SELECT
    E.[EmployeeId],
    E.[BaseSalary],
    E.[HireDate],
    E.[CreatedByUserId]
FROM [HumanResources].[Employees] AS E
WHERE NOT EXISTS
(
    SELECT 1
    FROM [HumanResources].[EmployeeSalaryHistory] AS ESH
    WHERE ESH.[EmployeeId] = E.[EmployeeId]
);
