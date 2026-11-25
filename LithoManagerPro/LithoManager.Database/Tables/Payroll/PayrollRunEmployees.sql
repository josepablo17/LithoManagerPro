-- One row per employee included in a payroll run. Identification, name,
-- department, job title and salary are snapshots taken at calculation
-- time so that closed payrolls do not change when the employee does.
CREATE TABLE [Payroll].[PayrollRunEmployees]
(
    [PayrollRunEmployeeId] int IDENTITY(1,1) NOT NULL,
    [PayrollRunId] int NOT NULL,
    [EmployeeId] int NOT NULL,
    [IdentificationNumber] nvarchar(30) NOT NULL,
    [EmployeeName] nvarchar(260) NOT NULL,
    [DepartmentName] nvarchar(100) NULL,
    [JobTitle] nvarchar(100) NULL,
    [MonthlyBaseSalary] decimal(18,2) NOT NULL,
    [PaidDays] decimal(5,2) NOT NULL,
    [GrossAmount] decimal(18,2) NOT NULL,
    [EmployeeContributionsAmount] decimal(18,2) NOT NULL,
    [IncomeTaxAmount] decimal(18,2) NOT NULL,
    [NetAmount] decimal(18,2) NOT NULL,
    [EmployerContributionsAmount] decimal(18,2) NOT NULL,

    CONSTRAINT [PkPayrollRunEmployees]
        PRIMARY KEY CLUSTERED ([PayrollRunEmployeeId]),

    CONSTRAINT [FkPayrollRunEmployeesPayrollRuns]
        FOREIGN KEY ([PayrollRunId])
        REFERENCES [Payroll].[PayrollRuns] ([PayrollRunId]),

    CONSTRAINT [FkPayrollRunEmployeesEmployees]
        FOREIGN KEY ([EmployeeId])
        REFERENCES [HumanResources].[Employees] ([EmployeeId]),

    CONSTRAINT [UqPayrollRunEmployeesRunEmployee]
        UNIQUE ([PayrollRunId], [EmployeeId]),

    CONSTRAINT [CkPayrollRunEmployeesPaidDays]
        CHECK ([PaidDays] BETWEEN 0 AND 31),

    CONSTRAINT [CkPayrollRunEmployeesAmounts]
        CHECK
        (
            [MonthlyBaseSalary] >= 0
            AND [EmployeeContributionsAmount] >= 0
            AND [IncomeTaxAmount] >= 0
            AND [EmployerContributionsAmount] >= 0
        )
);
GO

CREATE NONCLUSTERED INDEX [IxPayrollRunEmployeesEmployee]
    ON [Payroll].[PayrollRunEmployees]
    (
        [EmployeeId],
        [PayrollRunId]
    );
GO
