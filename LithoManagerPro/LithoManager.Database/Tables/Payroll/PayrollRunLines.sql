-- Breakdown of each employee's payroll by concept. ReferenceCode holds
-- the detail behind the concept: the contribution code (SEM, IVM...),
-- the overtime rule code or the disability type code.
CREATE TABLE [Payroll].[PayrollRunLines]
(
    [PayrollRunLineId] int IDENTITY(1,1) NOT NULL,
    [PayrollRunEmployeeId] int NOT NULL,
    [PayrollConceptId] int NOT NULL,
    [ReferenceCode] nvarchar(50) NULL,
    [Description] nvarchar(200) NOT NULL,
    [Quantity] decimal(9,2) NULL,
    [Rate] decimal(18,6) NULL,
    [BaseAmount] decimal(18,2) NULL,
    [Amount] decimal(18,2) NOT NULL,
    [SortOrder] int NOT NULL,

    CONSTRAINT [PkPayrollRunLines]
        PRIMARY KEY CLUSTERED ([PayrollRunLineId]),

    CONSTRAINT [FkPayrollRunLinesPayrollRunEmployees]
        FOREIGN KEY ([PayrollRunEmployeeId])
        REFERENCES [Payroll].[PayrollRunEmployees] ([PayrollRunEmployeeId]),

    CONSTRAINT [FkPayrollRunLinesPayrollConcepts]
        FOREIGN KEY ([PayrollConceptId])
        REFERENCES [Payroll].[PayrollConcepts] ([PayrollConceptId]),

    CONSTRAINT [CkPayrollRunLinesDescriptionNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Description]))) > 0)
);
GO

CREATE NONCLUSTERED INDEX [IxPayrollRunLinesRunEmployee]
    ON [Payroll].[PayrollRunLines]
    (
        [PayrollRunEmployeeId],
        [SortOrder]
    );
GO
