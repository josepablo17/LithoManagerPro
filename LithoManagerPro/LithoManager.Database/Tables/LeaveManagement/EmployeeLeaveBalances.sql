CREATE TABLE [LeaveManagement].[EmployeeLeaveBalances]
(
    [EmployeeLeaveBalanceId] int IDENTITY(1,1) NOT NULL,
    [EmployeeId] int NOT NULL,
    [LeaveTypeId] int NOT NULL,
    [LeavePolicyId] int NOT NULL,

    [AccruedDays] decimal(9,2) NOT NULL
        CONSTRAINT [DfEmployeeLeaveBalancesAccruedDays]
        DEFAULT (0),

    [AdjustedDays] decimal(9,2) NOT NULL
        CONSTRAINT [DfEmployeeLeaveBalancesAdjustedDays]
        DEFAULT (0),

    [PendingDays] decimal(9,2) NOT NULL
        CONSTRAINT [DfEmployeeLeaveBalancesPendingDays]
        DEFAULT (0),

    [UsedDays] decimal(9,2) NOT NULL
        CONSTRAINT [DfEmployeeLeaveBalancesUsedDays]
        DEFAULT (0),

    [AvailableDays] AS
        CONVERT(decimal(9,2), [AccruedDays] + [AdjustedDays] - [PendingDays] - [UsedDays])
        PERSISTED,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfEmployeeLeaveBalancesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkEmployeeLeaveBalances]
        PRIMARY KEY CLUSTERED ([EmployeeLeaveBalanceId]),

    CONSTRAINT [FkEmployeeLeaveBalancesEmployeesEmployeeId]
        FOREIGN KEY ([EmployeeId])
        REFERENCES [HumanResources].[Employees] ([EmployeeId]),

    CONSTRAINT [FkEmployeeLeaveBalancesLeaveTypesLeaveTypeId]
        FOREIGN KEY ([LeaveTypeId])
        REFERENCES [LeaveManagement].[LeaveTypes] ([LeaveTypeId]),

    CONSTRAINT [FkEmployeeLeaveBalancesLeavePoliciesLeavePolicyId]
        FOREIGN KEY ([LeavePolicyId], [LeaveTypeId])
        REFERENCES [LeaveManagement].[LeavePolicies] ([LeavePolicyId], [LeaveTypeId]),

    CONSTRAINT [FkEmployeeLeaveBalancesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkEmployeeLeaveBalancesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqEmployeeLeaveBalancesEmployeeIdLeaveTypeId]
        UNIQUE ([EmployeeId], [LeaveTypeId]),

    CONSTRAINT [CkEmployeeLeaveBalancesAccruedDaysNonNegative]
        CHECK ([AccruedDays] >= 0),

    CONSTRAINT [CkEmployeeLeaveBalancesPendingDaysNonNegative]
        CHECK ([PendingDays] >= 0),

    CONSTRAINT [CkEmployeeLeaveBalancesUsedDaysNonNegative]
        CHECK ([UsedDays] >= 0),

    CONSTRAINT [CkEmployeeLeaveBalancesTotalDaysNonNegative]
        CHECK ([AccruedDays] + [AdjustedDays] >= 0),

    CONSTRAINT [CkEmployeeLeaveBalancesAvailableDaysNonNegative]
        CHECK ([AccruedDays] + [AdjustedDays] - [PendingDays] - [UsedDays] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IxEmployeeLeaveBalancesEmployeeId]
    ON [LeaveManagement].[EmployeeLeaveBalances]
    (
        [EmployeeId]
    )
    INCLUDE
    (
        [LeaveTypeId],
        [LeavePolicyId],
        [AccruedDays],
        [AdjustedDays],
        [PendingDays],
        [UsedDays]
    );
GO
