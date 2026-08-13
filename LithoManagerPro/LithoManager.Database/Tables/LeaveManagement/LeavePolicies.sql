CREATE TABLE [LeaveManagement].[LeavePolicies]
(
    [LeavePolicyId] int IDENTITY(1,1) NOT NULL,
    [LeaveTypeId] int NOT NULL,
    [LeavePolicyCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [EntitlementDays] decimal(9,2) NOT NULL,
    [EntitlementWeeks] smallint NOT NULL,

    [UsesBusinessDays] bit NOT NULL
        CONSTRAINT [DfLeavePoliciesUsesBusinessDays]
        DEFAULT (1),

    [IsActive] bit NOT NULL
        CONSTRAINT [DfLeavePoliciesIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfLeavePoliciesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkLeavePolicies]
        PRIMARY KEY CLUSTERED ([LeavePolicyId]),

    CONSTRAINT [FkLeavePoliciesLeaveTypesLeaveTypeId]
        FOREIGN KEY ([LeaveTypeId])
        REFERENCES [LeaveManagement].[LeaveTypes] ([LeaveTypeId]),

    CONSTRAINT [FkLeavePoliciesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkLeavePoliciesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqLeavePoliciesLeavePolicyCode]
        UNIQUE ([LeavePolicyCode]),

    CONSTRAINT [UqLeavePoliciesName]
        UNIQUE ([Name]),

    CONSTRAINT [UqLeavePoliciesLeavePolicyIdLeaveTypeId]
        UNIQUE ([LeavePolicyId], [LeaveTypeId]),

    CONSTRAINT [CkLeavePoliciesLeavePolicyCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([LeavePolicyCode]))) > 0),

    CONSTRAINT [CkLeavePoliciesLeavePolicyCodeTrimmed]
        CHECK ([LeavePolicyCode] = LTRIM(RTRIM([LeavePolicyCode]))),

    CONSTRAINT [CkLeavePoliciesLeavePolicyCodeNoSpaces]
        CHECK ([LeavePolicyCode] NOT LIKE N'% %'),

    CONSTRAINT [CkLeavePoliciesNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

    CONSTRAINT [CkLeavePoliciesNameTrimmed]
        CHECK ([Name] = LTRIM(RTRIM([Name]))),

    CONSTRAINT [CkLeavePoliciesEntitlementDaysPositive]
        CHECK ([EntitlementDays] > 0),

    CONSTRAINT [CkLeavePoliciesEntitlementWeeksPositive]
        CHECK ([EntitlementWeeks] > 0)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxLeavePoliciesLeaveTypeIdActive]
    ON [LeaveManagement].[LeavePolicies]
    (
        [LeaveTypeId]
    )
    WHERE [IsActive] = 1;
GO
