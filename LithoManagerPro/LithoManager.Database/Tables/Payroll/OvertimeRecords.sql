CREATE TABLE [Payroll].[OvertimeRecords]
(
    [OvertimeRecordId] int IDENTITY(1,1) NOT NULL,
    [EmployeeId] int NOT NULL,
    [AttendanceRecordId] int NULL,
    [OvertimeRuleId] int NOT NULL,
    [OvertimeDate] date NOT NULL,
    [Hours] decimal(5,2) NOT NULL,
    [ApprovalStatus] nvarchar(30) NOT NULL
        CONSTRAINT [DfOvertimeRecordsApprovalStatus]
        DEFAULT (N'Pending'),

    [ApprovedAtUtc] datetime2(3) NULL,
    [ApprovedByUserId] int NULL,
    [RejectedAtUtc] datetime2(3) NULL,
    [RejectedByUserId] int NULL,
    [RejectionReason] nvarchar(300) NULL,
    [Notes] nvarchar(500) NULL,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfOvertimeRecordsCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkOvertimeRecords]
        PRIMARY KEY CLUSTERED ([OvertimeRecordId]),

    CONSTRAINT [FkOvertimeRecordsEmployeesEmployeeId]
        FOREIGN KEY ([EmployeeId])
        REFERENCES [HumanResources].[Employees] ([EmployeeId]),

    CONSTRAINT [FkOvertimeRecordsAttendanceRecordsAttendanceRecordId]
        FOREIGN KEY ([AttendanceRecordId])
        REFERENCES [Payroll].[AttendanceRecords] ([AttendanceRecordId]),

    CONSTRAINT [FkOvertimeRecordsOvertimeRulesOvertimeRuleId]
        FOREIGN KEY ([OvertimeRuleId])
        REFERENCES [Payroll].[OvertimeRules] ([OvertimeRuleId]),

    CONSTRAINT [FkOvertimeRecordsUsersApprovedByUserId]
        FOREIGN KEY ([ApprovedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkOvertimeRecordsUsersRejectedByUserId]
        FOREIGN KEY ([RejectedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkOvertimeRecordsUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkOvertimeRecordsUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [CkOvertimeRecordsHours]
        CHECK ([Hours] > 0 AND [Hours] <= 24),

    CONSTRAINT [CkOvertimeRecordsApprovalStatus]
        CHECK
        (
            [ApprovalStatus] IN
            (
                N'Pending',
                N'Approved',
                N'Rejected',
                N'Cancelled'
            )
        ),

    CONSTRAINT [CkOvertimeRecordsApprovalFields]
        CHECK
        (
            (
                [ApprovalStatus] <> N'Approved'
                AND [ApprovedAtUtc] IS NULL
                AND [ApprovedByUserId] IS NULL
            )
            OR
            (
                [ApprovalStatus] = N'Approved'
                AND [ApprovedAtUtc] IS NOT NULL
                AND [ApprovedByUserId] IS NOT NULL
                AND [RejectedAtUtc] IS NULL
                AND [RejectedByUserId] IS NULL
                AND [RejectionReason] IS NULL
            )
        ),

    CONSTRAINT [CkOvertimeRecordsRejectionFields]
        CHECK
        (
            (
                [ApprovalStatus] <> N'Rejected'
                AND [RejectedAtUtc] IS NULL
                AND [RejectedByUserId] IS NULL
                AND [RejectionReason] IS NULL
            )
            OR
            (
                [ApprovalStatus] = N'Rejected'
                AND [RejectedAtUtc] IS NOT NULL
                AND [RejectedByUserId] IS NOT NULL
                AND LEN(LTRIM(RTRIM([RejectionReason]))) > 0
                AND [ApprovedAtUtc] IS NULL
                AND [ApprovedByUserId] IS NULL
            )
        ),

    CONSTRAINT [CkOvertimeRecordsRejectionReasonTrimmed]
        CHECK
        (
            [RejectionReason] IS NULL
            OR [RejectionReason] = LTRIM(RTRIM([RejectionReason]))
        ),

    CONSTRAINT [CkOvertimeRecordsNotesNotBlank]
        CHECK
        (
            [Notes] IS NULL
            OR LEN(LTRIM(RTRIM([Notes]))) > 0
        ),

    CONSTRAINT [CkOvertimeRecordsNotesTrimmed]
        CHECK
        (
            [Notes] IS NULL
            OR [Notes] = LTRIM(RTRIM([Notes]))
        )
);
GO

CREATE NONCLUSTERED INDEX [IxOvertimeRecordsEmployeeIdOvertimeDate]
    ON [Payroll].[OvertimeRecords]
    (
        [EmployeeId],
        [OvertimeDate]
    )
    INCLUDE
    (
        [OvertimeRuleId],
        [Hours],
        [ApprovalStatus]
    );
GO

CREATE NONCLUSTERED INDEX [IxOvertimeRecordsApprovalStatusOvertimeDate]
    ON [Payroll].[OvertimeRecords]
    (
        [ApprovalStatus],
        [OvertimeDate]
    )
    INCLUDE
    (
        [EmployeeId],
        [Hours],
        [OvertimeRuleId]
    );
GO
