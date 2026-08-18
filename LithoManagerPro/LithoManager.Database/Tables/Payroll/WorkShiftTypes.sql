CREATE TABLE [Payroll].[WorkShiftTypes]
(
    [WorkShiftTypeId] int IDENTITY(1,1) NOT NULL,
    [WorkShiftTypeCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [MaxOrdinaryHoursPerDay] decimal(5,2) NOT NULL,
    [MaxOrdinaryHoursPerWeek] decimal(5,2) NOT NULL,
    [MaxTotalHoursPerDay] decimal(5,2) NOT NULL,
    [EffectiveFromDate] date NOT NULL,
    [EffectiveToDate] date NULL,

    [IsActive] bit NOT NULL
        CONSTRAINT [DfWorkShiftTypesIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfWorkShiftTypesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkWorkShiftTypes]
        PRIMARY KEY CLUSTERED ([WorkShiftTypeId]),

    CONSTRAINT [FkWorkShiftTypesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkWorkShiftTypesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqWorkShiftTypesCodeEffectiveFrom]
        UNIQUE ([WorkShiftTypeCode], [EffectiveFromDate]),

    CONSTRAINT [CkWorkShiftTypesWorkShiftTypeCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([WorkShiftTypeCode]))) > 0),

    CONSTRAINT [CkWorkShiftTypesWorkShiftTypeCodeTrimmed]
        CHECK ([WorkShiftTypeCode] = LTRIM(RTRIM([WorkShiftTypeCode]))),

    CONSTRAINT [CkWorkShiftTypesWorkShiftTypeCodeNoSpaces]
        CHECK ([WorkShiftTypeCode] NOT LIKE N'% %'),

    CONSTRAINT [CkWorkShiftTypesNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

    CONSTRAINT [CkWorkShiftTypesNameTrimmed]
        CHECK ([Name] = LTRIM(RTRIM([Name]))),

    CONSTRAINT [CkWorkShiftTypesOrdinaryHoursPositive]
        CHECK
        (
            [MaxOrdinaryHoursPerDay] > 0
            AND [MaxOrdinaryHoursPerWeek] > 0
        ),

    CONSTRAINT [CkWorkShiftTypesTotalHours]
        CHECK ([MaxTotalHoursPerDay] >= [MaxOrdinaryHoursPerDay]),

    CONSTRAINT [CkWorkShiftTypesEffectiveDates]
        CHECK
        (
            [EffectiveToDate] IS NULL
            OR [EffectiveToDate] >= [EffectiveFromDate]
        )
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxWorkShiftTypesCodeCurrent]
    ON [Payroll].[WorkShiftTypes]
    (
        [WorkShiftTypeCode]
    )
    WHERE [EffectiveToDate] IS NULL;
GO
