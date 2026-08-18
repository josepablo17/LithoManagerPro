CREATE TABLE [Payroll].[AguinaldoRules]
(
    [AguinaldoRuleId] int IDENTITY(1,1) NOT NULL,
    [AguinaldoRuleCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [CalculationStartMonth] tinyint NOT NULL,
    [CalculationStartDay] tinyint NOT NULL,
    [CalculationEndMonth] tinyint NOT NULL,
    [CalculationEndDay] tinyint NOT NULL,
    [Divisor] smallint NOT NULL,
    [PaymentDueMonth] tinyint NOT NULL,
    [PaymentDueDay] tinyint NOT NULL,

    [IncludesOrdinarySalary] bit NOT NULL
        CONSTRAINT [DfAguinaldoRulesIncludesOrdinarySalary]
        DEFAULT (1),

    [IncludesOvertime] bit NOT NULL
        CONSTRAINT [DfAguinaldoRulesIncludesOvertime]
        DEFAULT (1),

    [IncludesSalaryInKind] bit NOT NULL
        CONSTRAINT [DfAguinaldoRulesIncludesSalaryInKind]
        DEFAULT (1),

    [ExcludesCommonIllnessSubsidy] bit NOT NULL
        CONSTRAINT [DfAguinaldoRulesExcludesCommonIllnessSubsidy]
        DEFAULT (1),

    [IncludesMaternitySubsidy] bit NOT NULL
        CONSTRAINT [DfAguinaldoRulesIncludesMaternitySubsidy]
        DEFAULT (1),

    [EffectiveFromDate] date NOT NULL,
    [EffectiveToDate] date NULL,

    [IsActive] bit NOT NULL
        CONSTRAINT [DfAguinaldoRulesIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfAguinaldoRulesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkAguinaldoRules]
        PRIMARY KEY CLUSTERED ([AguinaldoRuleId]),

    CONSTRAINT [FkAguinaldoRulesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkAguinaldoRulesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqAguinaldoRulesCodeEffectiveFrom]
        UNIQUE ([AguinaldoRuleCode], [EffectiveFromDate]),

    CONSTRAINT [CkAguinaldoRulesAguinaldoRuleCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([AguinaldoRuleCode]))) > 0),

    CONSTRAINT [CkAguinaldoRulesAguinaldoRuleCodeTrimmed]
        CHECK ([AguinaldoRuleCode] = LTRIM(RTRIM([AguinaldoRuleCode]))),

    CONSTRAINT [CkAguinaldoRulesAguinaldoRuleCodeNoSpaces]
        CHECK ([AguinaldoRuleCode] NOT LIKE N'% %'),

    CONSTRAINT [CkAguinaldoRulesNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

    CONSTRAINT [CkAguinaldoRulesNameTrimmed]
        CHECK ([Name] = LTRIM(RTRIM([Name]))),

    CONSTRAINT [CkAguinaldoRulesCalculationStartMonth]
        CHECK ([CalculationStartMonth] BETWEEN 1 AND 12),

    CONSTRAINT [CkAguinaldoRulesCalculationStartDay]
        CHECK ([CalculationStartDay] BETWEEN 1 AND 31),

    CONSTRAINT [CkAguinaldoRulesCalculationEndMonth]
        CHECK ([CalculationEndMonth] BETWEEN 1 AND 12),

    CONSTRAINT [CkAguinaldoRulesCalculationEndDay]
        CHECK ([CalculationEndDay] BETWEEN 1 AND 31),

    CONSTRAINT [CkAguinaldoRulesPaymentDueMonth]
        CHECK ([PaymentDueMonth] BETWEEN 1 AND 12),

    CONSTRAINT [CkAguinaldoRulesPaymentDueDay]
        CHECK ([PaymentDueDay] BETWEEN 1 AND 31),

    CONSTRAINT [CkAguinaldoRulesDivisorPositive]
        CHECK ([Divisor] > 0),

    CONSTRAINT [CkAguinaldoRulesEffectiveDates]
        CHECK
        (
            [EffectiveToDate] IS NULL
            OR [EffectiveToDate] >= [EffectiveFromDate]
        )
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxAguinaldoRulesCodeCurrent]
    ON [Payroll].[AguinaldoRules]
    (
        [AguinaldoRuleCode]
    )
    WHERE [EffectiveToDate] IS NULL;
GO
