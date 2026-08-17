CREATE TABLE [HumanResources].[EmployeeSalaryHistory]
(
    [EmployeeSalaryHistoryId] int IDENTITY(1,1) NOT NULL,
    [EmployeeId] int NOT NULL,
    [BaseSalary] decimal(18,2) NOT NULL,
    [EffectiveFromDate] date NOT NULL,
    [EffectiveToDate] date NULL,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfEmployeeSalaryHistoryCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkEmployeeSalaryHistory]
        PRIMARY KEY CLUSTERED ([EmployeeSalaryHistoryId]),

    CONSTRAINT [FkEmployeeSalaryHistoryEmployeesEmployeeId]
        FOREIGN KEY ([EmployeeId])
        REFERENCES [HumanResources].[Employees] ([EmployeeId]),

    CONSTRAINT [FkEmployeeSalaryHistoryUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkEmployeeSalaryHistoryUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [CkEmployeeSalaryHistoryBaseSalaryNonNegative]
        CHECK ([BaseSalary] >= 0),

    CONSTRAINT [CkEmployeeSalaryHistoryEffectiveDates]
        CHECK
        (
            [EffectiveToDate] IS NULL
            OR [EffectiveToDate] >= [EffectiveFromDate]
        )
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxEmployeeSalaryHistoryEmployeeIdEffectiveFromDate]
    ON [HumanResources].[EmployeeSalaryHistory]
    (
        [EmployeeId],
        [EffectiveFromDate]
    );
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxEmployeeSalaryHistoryEmployeeIdCurrent]
    ON [HumanResources].[EmployeeSalaryHistory]
    (
        [EmployeeId]
    )
    WHERE [EffectiveToDate] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IxEmployeeSalaryHistoryEmployeeIdEffectiveFromDate]
    ON [HumanResources].[EmployeeSalaryHistory]
    (
        [EmployeeId],
        [EffectiveFromDate] DESC
    )
    INCLUDE
    (
        [EffectiveToDate],
        [BaseSalary]
    );
GO
