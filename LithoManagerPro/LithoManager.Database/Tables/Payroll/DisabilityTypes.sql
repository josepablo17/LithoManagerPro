CREATE TABLE [Payroll].[DisabilityTypes]
(
    [DisabilityTypeId] int IDENTITY(1,1) NOT NULL,
    [DisabilityTypeCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,

    [CountsAsSalaryForAguinaldo] bit NOT NULL
        CONSTRAINT [DfDisabilityTypesCountsAsSalaryForAguinaldo]
        DEFAULT (0),

    [RequiresSubsidyTracking] bit NOT NULL
        CONSTRAINT [DfDisabilityTypesRequiresSubsidyTracking]
        DEFAULT (1),

    [ReducesWorkedDays] bit NOT NULL
        CONSTRAINT [DfDisabilityTypesReducesWorkedDays]
        DEFAULT (1),

    [IsActive] bit NOT NULL
        CONSTRAINT [DfDisabilityTypesIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfDisabilityTypesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkDisabilityTypes]
        PRIMARY KEY CLUSTERED ([DisabilityTypeId]),

    CONSTRAINT [FkDisabilityTypesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkDisabilityTypesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqDisabilityTypesDisabilityTypeCode]
        UNIQUE ([DisabilityTypeCode]),

    CONSTRAINT [UqDisabilityTypesName]
        UNIQUE ([Name]),

    CONSTRAINT [CkDisabilityTypesDisabilityTypeCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([DisabilityTypeCode]))) > 0),

    CONSTRAINT [CkDisabilityTypesDisabilityTypeCodeTrimmed]
        CHECK ([DisabilityTypeCode] = LTRIM(RTRIM([DisabilityTypeCode]))),

    CONSTRAINT [CkDisabilityTypesDisabilityTypeCodeNoSpaces]
        CHECK ([DisabilityTypeCode] NOT LIKE N'% %'),

    CONSTRAINT [CkDisabilityTypesNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

    CONSTRAINT [CkDisabilityTypesNameTrimmed]
        CHECK ([Name] = LTRIM(RTRIM([Name])))
);
GO

CREATE NONCLUSTERED INDEX [IxDisabilityTypesIsActive]
    ON [Payroll].[DisabilityTypes]
    (
        [IsActive]
    )
    INCLUDE
    (
        [DisabilityTypeCode],
        [Name],
        [CountsAsSalaryForAguinaldo],
        [RequiresSubsidyTracking],
        [ReducesWorkedDays]
    );
GO
