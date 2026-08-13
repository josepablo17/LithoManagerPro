CREATE TABLE [LeaveManagement].[LeaveTypes]
(
    [LeaveTypeId] int IDENTITY(1,1) NOT NULL,
    [LeaveTypeCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [AffectsVacationBalance] bit NOT NULL
        CONSTRAINT [DfLeaveTypesAffectsVacationBalance]
        DEFAULT (0),

    [IsActive] bit NOT NULL
        CONSTRAINT [DfLeaveTypesIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfLeaveTypesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkLeaveTypes]
        PRIMARY KEY CLUSTERED ([LeaveTypeId]),

    CONSTRAINT [FkLeaveTypesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkLeaveTypesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqLeaveTypesLeaveTypeCode]
        UNIQUE ([LeaveTypeCode]),

    CONSTRAINT [UqLeaveTypesName]
        UNIQUE ([Name]),

    CONSTRAINT [CkLeaveTypesLeaveTypeCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([LeaveTypeCode]))) > 0),

    CONSTRAINT [CkLeaveTypesLeaveTypeCodeTrimmed]
        CHECK ([LeaveTypeCode] = LTRIM(RTRIM([LeaveTypeCode]))),

    CONSTRAINT [CkLeaveTypesLeaveTypeCodeNoSpaces]
        CHECK ([LeaveTypeCode] NOT LIKE N'% %'),

    CONSTRAINT [CkLeaveTypesNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

    CONSTRAINT [CkLeaveTypesNameTrimmed]
        CHECK ([Name] = LTRIM(RTRIM([Name])))
);
GO

CREATE NONCLUSTERED INDEX [IxLeaveTypesIsActive]
    ON [LeaveManagement].[LeaveTypes]
    (
        [IsActive]
    )
    INCLUDE
    (
        [LeaveTypeCode],
        [Name],
        [AffectsVacationBalance]
    );
GO
