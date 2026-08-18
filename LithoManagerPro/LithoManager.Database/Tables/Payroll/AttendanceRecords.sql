CREATE TABLE [Payroll].[AttendanceRecords]
(
    [AttendanceRecordId] int IDENTITY(1,1) NOT NULL,
    [EmployeeId] int NOT NULL,
    [WorkShiftTypeId] int NOT NULL,
    [AttendanceDate] date NOT NULL,
    [AttendanceStatus] nvarchar(30) NOT NULL,
    [ExpectedHours] decimal(5,2) NOT NULL,
    [WorkedHours] decimal(5,2) NOT NULL,
    [PaidHours] decimal(5,2) NOT NULL,
    [UnpaidHours] decimal(5,2) NOT NULL,

    [IsPaidHoliday] bit NOT NULL
        CONSTRAINT [DfAttendanceRecordsIsPaidHoliday]
        DEFAULT (0),

    [IsApproved] bit NOT NULL
        CONSTRAINT [DfAttendanceRecordsIsApproved]
        DEFAULT (0),

    [ApprovedAtUtc] datetime2(3) NULL,
    [ApprovedByUserId] int NULL,
    [Notes] nvarchar(500) NULL,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfAttendanceRecordsCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkAttendanceRecords]
        PRIMARY KEY CLUSTERED ([AttendanceRecordId]),

    CONSTRAINT [FkAttendanceRecordsEmployeesEmployeeId]
        FOREIGN KEY ([EmployeeId])
        REFERENCES [HumanResources].[Employees] ([EmployeeId]),

    CONSTRAINT [FkAttendanceRecordsWorkShiftTypesWorkShiftTypeId]
        FOREIGN KEY ([WorkShiftTypeId])
        REFERENCES [Payroll].[WorkShiftTypes] ([WorkShiftTypeId]),

    CONSTRAINT [FkAttendanceRecordsUsersApprovedByUserId]
        FOREIGN KEY ([ApprovedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkAttendanceRecordsUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkAttendanceRecordsUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqAttendanceRecordsEmployeeIdAttendanceDate]
        UNIQUE ([EmployeeId], [AttendanceDate]),

    CONSTRAINT [CkAttendanceRecordsAttendanceStatus]
        CHECK
        (
            [AttendanceStatus] IN
            (
                N'Present',
                N'Partial',
                N'Absent',
                N'Holiday',
                N'Leave',
                N'Disability'
            )
        ),

    CONSTRAINT [CkAttendanceRecordsHoursNonNegative]
        CHECK
        (
            [ExpectedHours] >= 0
            AND [WorkedHours] >= 0
            AND [PaidHours] >= 0
            AND [UnpaidHours] >= 0
        ),

    CONSTRAINT [CkAttendanceRecordsWorkedHoursLimit]
        CHECK ([WorkedHours] <= 24),

    CONSTRAINT [CkAttendanceRecordsExpectedHoursLimit]
        CHECK ([ExpectedHours] <= 24),

    CONSTRAINT [CkAttendanceRecordsPaidHoursLimit]
        CHECK ([PaidHours] <= 24),

    CONSTRAINT [CkAttendanceRecordsUnpaidHoursLimit]
        CHECK ([UnpaidHours] <= 24),

    CONSTRAINT [CkAttendanceRecordsApprovalFields]
        CHECK
        (
            (
                [IsApproved] = 0
                AND [ApprovedAtUtc] IS NULL
                AND [ApprovedByUserId] IS NULL
            )
            OR
            (
                [IsApproved] = 1
                AND [ApprovedAtUtc] IS NOT NULL
                AND [ApprovedByUserId] IS NOT NULL
            )
        ),

    CONSTRAINT [CkAttendanceRecordsNotesNotBlank]
        CHECK
        (
            [Notes] IS NULL
            OR LEN(LTRIM(RTRIM([Notes]))) > 0
        ),

    CONSTRAINT [CkAttendanceRecordsNotesTrimmed]
        CHECK
        (
            [Notes] IS NULL
            OR [Notes] = LTRIM(RTRIM([Notes]))
        )
);
GO

CREATE NONCLUSTERED INDEX [IxAttendanceRecordsEmployeeIdAttendanceDate]
    ON [Payroll].[AttendanceRecords]
    (
        [EmployeeId],
        [AttendanceDate]
    )
    INCLUDE
    (
        [AttendanceStatus],
        [ExpectedHours],
        [WorkedHours],
        [PaidHours],
        [UnpaidHours],
        [IsApproved]
    );
GO

CREATE NONCLUSTERED INDEX [IxAttendanceRecordsAttendanceDateIsApproved]
    ON [Payroll].[AttendanceRecords]
    (
        [AttendanceDate],
        [IsApproved]
    )
    INCLUDE
    (
        [EmployeeId],
        [AttendanceStatus],
        [WorkedHours],
        [PaidHours]
    );
GO
