CREATE TABLE [Payroll].[PayrollRuns]
(
    [PayrollRunId] int IDENTITY(1,1) NOT NULL,
    [RunType] nvarchar(20) NOT NULL,
    [PeriodYear] smallint NOT NULL,
    [PeriodMonth] tinyint NOT NULL,
    [PeriodStartDate] date NOT NULL,
    [PeriodEndDate] date NOT NULL,
    [PaymentDate] date NOT NULL,

    [Status] nvarchar(20) NOT NULL
        CONSTRAINT [DfPayrollRunsStatus]
        DEFAULT (N'Open'),

    [EmployeeCount] int NOT NULL
        CONSTRAINT [DfPayrollRunsEmployeeCount]
        DEFAULT (0),

    [TotalGrossAmount] decimal(18,2) NOT NULL
        CONSTRAINT [DfPayrollRunsTotalGrossAmount]
        DEFAULT (0),

    [TotalEmployeeContributionsAmount] decimal(18,2) NOT NULL
        CONSTRAINT [DfPayrollRunsTotalEmployeeContributionsAmount]
        DEFAULT (0),

    [TotalIncomeTaxAmount] decimal(18,2) NOT NULL
        CONSTRAINT [DfPayrollRunsTotalIncomeTaxAmount]
        DEFAULT (0),

    [TotalNetAmount] decimal(18,2) NOT NULL
        CONSTRAINT [DfPayrollRunsTotalNetAmount]
        DEFAULT (0),

    [TotalEmployerContributionsAmount] decimal(18,2) NOT NULL
        CONSTRAINT [DfPayrollRunsTotalEmployerContributionsAmount]
        DEFAULT (0),

    [CalculatedAtUtc] datetime2(3) NULL,
    [CalculatedByUserId] int NULL,
    [ClosedAtUtc] datetime2(3) NULL,
    [ClosedByUserId] int NULL,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfPayrollRunsCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkPayrollRuns]
        PRIMARY KEY CLUSTERED ([PayrollRunId]),

    CONSTRAINT [FkPayrollRunsUsersCalculatedByUserId]
        FOREIGN KEY ([CalculatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkPayrollRunsUsersClosedByUserId]
        FOREIGN KEY ([ClosedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkPayrollRunsUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkPayrollRunsUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    -- One regular payroll per month and one aguinaldo per year
    -- (aguinaldo runs always use PeriodMonth = 12).
    CONSTRAINT [UqPayrollRunsTypePeriod]
        UNIQUE ([RunType], [PeriodYear], [PeriodMonth]),

    CONSTRAINT [CkPayrollRunsRunType]
        CHECK ([RunType] IN (N'Regular', N'Aguinaldo')),

    CONSTRAINT [CkPayrollRunsStatus]
        CHECK ([Status] IN (N'Open', N'Calculated', N'Closed')),

    CONSTRAINT [CkPayrollRunsPeriodYear]
        CHECK ([PeriodYear] BETWEEN 2000 AND 2100),

    CONSTRAINT [CkPayrollRunsPeriodMonth]
        CHECK ([PeriodMonth] BETWEEN 1 AND 12),

    CONSTRAINT [CkPayrollRunsAguinaldoMonth]
        CHECK ([RunType] <> N'Aguinaldo' OR [PeriodMonth] = 12),

    CONSTRAINT [CkPayrollRunsPeriodDates]
        CHECK ([PeriodEndDate] >= [PeriodStartDate]),

    CONSTRAINT [CkPayrollRunsEmployeeCount]
        CHECK ([EmployeeCount] >= 0),

    CONSTRAINT [CkPayrollRunsCalculated]
        CHECK
        (
            [Status] = N'Open'
            OR
            (
                [CalculatedAtUtc] IS NOT NULL
                AND [CalculatedByUserId] IS NOT NULL
            )
        ),

    CONSTRAINT [CkPayrollRunsClosed]
        CHECK
        (
            (
                [Status] = N'Closed'
                AND [ClosedAtUtc] IS NOT NULL
                AND [ClosedByUserId] IS NOT NULL
            )
            OR
            (
                [Status] <> N'Closed'
                AND [ClosedAtUtc] IS NULL
                AND [ClosedByUserId] IS NULL
            )
        )
);
GO

CREATE NONCLUSTERED INDEX [IxPayrollRunsPeriod]
    ON [Payroll].[PayrollRuns]
    (
        [PeriodYear] DESC,
        [PeriodMonth] DESC
    )
    INCLUDE
    (
        [RunType],
        [Status]
    );
GO
