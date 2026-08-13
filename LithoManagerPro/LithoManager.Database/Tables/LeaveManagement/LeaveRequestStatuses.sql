CREATE TABLE [LeaveManagement].[LeaveRequestStatuses]
(
    [LeaveRequestStatusCode] nvarchar(30) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [SortOrder] smallint NOT NULL,
    [IsTerminal] bit NOT NULL
        CONSTRAINT [DfLeaveRequestStatusesIsTerminal]
        DEFAULT (0),

    [IsActive] bit NOT NULL
        CONSTRAINT [DfLeaveRequestStatusesIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfLeaveRequestStatusesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [UpdatedAtUtc] datetime2(3) NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkLeaveRequestStatuses]
        PRIMARY KEY CLUSTERED ([LeaveRequestStatusCode]),

    CONSTRAINT [UqLeaveRequestStatusesName]
        UNIQUE ([Name]),

    CONSTRAINT [UqLeaveRequestStatusesSortOrder]
        UNIQUE ([SortOrder]),

    CONSTRAINT [CkLeaveRequestStatusesLeaveRequestStatusCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([LeaveRequestStatusCode]))) > 0),

    CONSTRAINT [CkLeaveRequestStatusesLeaveRequestStatusCodeTrimmed]
        CHECK ([LeaveRequestStatusCode] = LTRIM(RTRIM([LeaveRequestStatusCode]))),

    CONSTRAINT [CkLeaveRequestStatusesLeaveRequestStatusCodeNoSpaces]
        CHECK ([LeaveRequestStatusCode] NOT LIKE N'% %'),

    CONSTRAINT [CkLeaveRequestStatusesNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

    CONSTRAINT [CkLeaveRequestStatusesNameTrimmed]
        CHECK ([Name] = LTRIM(RTRIM([Name]))),

    CONSTRAINT [CkLeaveRequestStatusesSortOrderPositive]
        CHECK ([SortOrder] > 0)
);
GO
