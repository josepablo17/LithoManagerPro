CREATE TABLE [Documents].[EmployeeRecords]
(
    [EmployeeRecordId] int IDENTITY(1,1) NOT NULL,
    [EmployeeId] int NOT NULL,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfEmployeeRecordsCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkEmployeeRecords]
        PRIMARY KEY CLUSTERED ([EmployeeRecordId]),

    CONSTRAINT [FkEmployeeRecordsEmployeesEmployeeId]
        FOREIGN KEY ([EmployeeId])
        REFERENCES [HumanResources].[Employees] ([EmployeeId]),

    CONSTRAINT [FkEmployeeRecordsUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkEmployeeRecordsUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqEmployeeRecordsEmployeeId]
        UNIQUE ([EmployeeId])
);
GO
