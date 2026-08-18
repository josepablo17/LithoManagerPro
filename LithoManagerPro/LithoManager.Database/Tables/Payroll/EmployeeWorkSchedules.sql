CREATE TABLE [Payroll].[EmployeeWorkSchedules]
(
    [EmployeeWorkScheduleId] int IDENTITY(1,1) NOT NULL,
    [EmployeeId] int NOT NULL,
    [WorkShiftTypeId] int NOT NULL,
    [WeeklyOrdinaryHours] decimal(5,2) NOT NULL,

    [WorksMonday] bit NOT NULL
        CONSTRAINT [DfEmployeeWorkSchedulesWorksMonday]
        DEFAULT (1),

    [WorksTuesday] bit NOT NULL
        CONSTRAINT [DfEmployeeWorkSchedulesWorksTuesday]
        DEFAULT (1),

    [WorksWednesday] bit NOT NULL
        CONSTRAINT [DfEmployeeWorkSchedulesWorksWednesday]
        DEFAULT (1),

    [WorksThursday] bit NOT NULL
        CONSTRAINT [DfEmployeeWorkSchedulesWorksThursday]
        DEFAULT (1),

    [WorksFriday] bit NOT NULL
        CONSTRAINT [DfEmployeeWorkSchedulesWorksFriday]
        DEFAULT (1),

    [WorksSaturday] bit NOT NULL
        CONSTRAINT [DfEmployeeWorkSchedulesWorksSaturday]
        DEFAULT (0),

    [WorksSunday] bit NOT NULL
        CONSTRAINT [DfEmployeeWorkSchedulesWorksSunday]
        DEFAULT (0),

    [EffectiveFromDate] date NOT NULL,
    [EffectiveToDate] date NULL,

    [IsActive] bit NOT NULL
        CONSTRAINT [DfEmployeeWorkSchedulesIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfEmployeeWorkSchedulesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkEmployeeWorkSchedules]
        PRIMARY KEY CLUSTERED ([EmployeeWorkScheduleId]),

    CONSTRAINT [FkEmployeeWorkSchedulesEmployeesEmployeeId]
        FOREIGN KEY ([EmployeeId])
        REFERENCES [HumanResources].[Employees] ([EmployeeId]),

    CONSTRAINT [FkEmployeeWorkSchedulesWorkShiftTypesWorkShiftTypeId]
        FOREIGN KEY ([WorkShiftTypeId])
        REFERENCES [Payroll].[WorkShiftTypes] ([WorkShiftTypeId]),

    CONSTRAINT [FkEmployeeWorkSchedulesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkEmployeeWorkSchedulesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqEmployeeWorkSchedulesEmployeeIdEffectiveFrom]
        UNIQUE ([EmployeeId], [EffectiveFromDate]),

    CONSTRAINT [CkEmployeeWorkSchedulesWeeklyHoursPositive]
        CHECK ([WeeklyOrdinaryHours] > 0),

    CONSTRAINT [CkEmployeeWorkSchedulesAtLeastOneWorkDay]
        CHECK
        (
            [WorksMonday] = 1
            OR [WorksTuesday] = 1
            OR [WorksWednesday] = 1
            OR [WorksThursday] = 1
            OR [WorksFriday] = 1
            OR [WorksSaturday] = 1
            OR [WorksSunday] = 1
        ),

    CONSTRAINT [CkEmployeeWorkSchedulesEffectiveDates]
        CHECK
        (
            [EffectiveToDate] IS NULL
            OR [EffectiveToDate] >= [EffectiveFromDate]
        )
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxEmployeeWorkSchedulesEmployeeIdCurrent]
    ON [Payroll].[EmployeeWorkSchedules]
    (
        [EmployeeId]
    )
    WHERE [EffectiveToDate] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IxEmployeeWorkSchedulesEmployeeIdEffectiveDates]
    ON [Payroll].[EmployeeWorkSchedules]
    (
        [EmployeeId],
        [EffectiveFromDate] DESC
    )
    INCLUDE
    (
        [EffectiveToDate],
        [WorkShiftTypeId],
        [WeeklyOrdinaryHours],
        [IsActive]
    );
GO
