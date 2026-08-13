CREATE TABLE [LeaveManagement].[LeaveBalanceTransactions]
(
    [LeaveBalanceTransactionId] int IDENTITY(1,1) NOT NULL,
    [EmployeeLeaveBalanceId] int NOT NULL,
    [LeaveRequestId] int NULL,
    [TransactionTypeCode] nvarchar(50) NOT NULL,

    [AccruedDaysDelta] decimal(9,2) NOT NULL
        CONSTRAINT [DfLeaveBalanceTransactionsAccruedDaysDelta]
        DEFAULT (0),

    [AdjustedDaysDelta] decimal(9,2) NOT NULL
        CONSTRAINT [DfLeaveBalanceTransactionsAdjustedDaysDelta]
        DEFAULT (0),

    [PendingDaysDelta] decimal(9,2) NOT NULL
        CONSTRAINT [DfLeaveBalanceTransactionsPendingDaysDelta]
        DEFAULT (0),

    [UsedDaysDelta] decimal(9,2) NOT NULL
        CONSTRAINT [DfLeaveBalanceTransactionsUsedDaysDelta]
        DEFAULT (0),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfLeaveBalanceTransactionsCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NOT NULL,

    CONSTRAINT [PkLeaveBalanceTransactions]
        PRIMARY KEY CLUSTERED ([LeaveBalanceTransactionId]),

    CONSTRAINT [FkLeaveBalanceTransactionsEmployeeLeaveBalancesEmployeeLeaveBalanceId]
        FOREIGN KEY ([EmployeeLeaveBalanceId])
        REFERENCES [LeaveManagement].[EmployeeLeaveBalances] ([EmployeeLeaveBalanceId]),

    CONSTRAINT [FkLeaveBalanceTransactionsLeaveRequestsLeaveRequestId]
        FOREIGN KEY ([LeaveRequestId])
        REFERENCES [LeaveManagement].[LeaveRequests] ([LeaveRequestId]),

    CONSTRAINT [FkLeaveBalanceTransactionsUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [CkLeaveBalanceTransactionsTransactionTypeCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([TransactionTypeCode]))) > 0),

    CONSTRAINT [CkLeaveBalanceTransactionsTransactionTypeCodeTrimmed]
        CHECK ([TransactionTypeCode] = LTRIM(RTRIM([TransactionTypeCode]))),

    CONSTRAINT [CkLeaveBalanceTransactionsTransactionTypeCodeNoSpaces]
        CHECK ([TransactionTypeCode] NOT LIKE N'% %'),

    CONSTRAINT [CkLeaveBalanceTransactionsTransactionTypeCode]
        CHECK
        (
            [TransactionTypeCode] IN
            (
                N'Accrual',
                N'Adjustment',
                N'PendingReservation',
                N'PendingRelease',
                N'Usage',
                N'UsageReversal'
            )
        ),

    CONSTRAINT [CkLeaveBalanceTransactionsHasDelta]
        CHECK
        (
            [AccruedDaysDelta] <> 0
            OR [AdjustedDaysDelta] <> 0
            OR [PendingDaysDelta] <> 0
            OR [UsedDaysDelta] <> 0
        )
);
GO

CREATE NONCLUSTERED INDEX [IxLeaveBalanceTransactionsEmployeeLeaveBalanceIdCreatedAtUtc]
    ON [LeaveManagement].[LeaveBalanceTransactions]
    (
        [EmployeeLeaveBalanceId],
        [CreatedAtUtc] DESC
    )
    INCLUDE
    (
        [LeaveRequestId],
        [TransactionTypeCode],
        [AccruedDaysDelta],
        [AdjustedDaysDelta],
        [PendingDaysDelta],
        [UsedDaysDelta]
    );
GO

CREATE NONCLUSTERED INDEX [IxLeaveBalanceTransactionsLeaveRequestId]
    ON [LeaveManagement].[LeaveBalanceTransactions]
    (
        [LeaveRequestId]
    )
    WHERE [LeaveRequestId] IS NOT NULL;
GO
