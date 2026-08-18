CREATE TABLE [Payroll].[OvertimeRules]
(
    [OvertimeRuleId] int IDENTITY(1,1) NOT NULL,
    [OvertimeRuleCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [HourMultiplier] decimal(9,4) NOT NULL,

    [CountsForAguinaldo] bit NOT NULL
        CONSTRAINT [DfOvertimeRulesCountsForAguinaldo]
        DEFAULT (1),

    [EffectiveFromDate] date NOT NULL,
    [EffectiveToDate] date NULL,

    [IsActive] bit NOT NULL
        CONSTRAINT [DfOvertimeRulesIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfOvertimeRulesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkOvertimeRules]
        PRIMARY KEY CLUSTERED ([OvertimeRuleId]),

    CONSTRAINT [FkOvertimeRulesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkOvertimeRulesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqOvertimeRulesCodeEffectiveFrom]
        UNIQUE ([OvertimeRuleCode], [EffectiveFromDate]),

    CONSTRAINT [CkOvertimeRulesOvertimeRuleCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([OvertimeRuleCode]))) > 0),

    CONSTRAINT [CkOvertimeRulesOvertimeRuleCodeTrimmed]
        CHECK ([OvertimeRuleCode] = LTRIM(RTRIM([OvertimeRuleCode]))),

    CONSTRAINT [CkOvertimeRulesOvertimeRuleCodeNoSpaces]
        CHECK ([OvertimeRuleCode] NOT LIKE N'% %'),

    CONSTRAINT [CkOvertimeRulesNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

    CONSTRAINT [CkOvertimeRulesNameTrimmed]
        CHECK ([Name] = LTRIM(RTRIM([Name]))),

    CONSTRAINT [CkOvertimeRulesHourMultiplier]
        CHECK ([HourMultiplier] >= 1),

    CONSTRAINT [CkOvertimeRulesEffectiveDates]
        CHECK
        (
            [EffectiveToDate] IS NULL
            OR [EffectiveToDate] >= [EffectiveFromDate]
        )
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxOvertimeRulesCodeCurrent]
    ON [Payroll].[OvertimeRules]
    (
        [OvertimeRuleCode]
    )
    WHERE [EffectiveToDate] IS NULL;
GO
