CREATE TABLE [Payroll].[IncomeTaxCredits]
(
    [IncomeTaxCreditId] int IDENTITY(1,1) NOT NULL,
    [CreditCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [TaxYear] smallint NOT NULL,
    [Periodicity] nvarchar(20) NOT NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    [EffectiveFromDate] date NOT NULL,
    [EffectiveToDate] date NULL,
    [LegalReference] nvarchar(300) NULL,

    [IsActive] bit NOT NULL
        CONSTRAINT [DfIncomeTaxCreditsIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfIncomeTaxCreditsCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkIncomeTaxCredits]
        PRIMARY KEY CLUSTERED ([IncomeTaxCreditId]),

    CONSTRAINT [FkIncomeTaxCreditsUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkIncomeTaxCreditsUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqIncomeTaxCreditsCodeYearPeriodicityEffectiveFrom]
        UNIQUE ([CreditCode], [TaxYear], [Periodicity], [EffectiveFromDate]),

    CONSTRAINT [CkIncomeTaxCreditsCreditCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([CreditCode]))) > 0),

    CONSTRAINT [CkIncomeTaxCreditsCreditCodeTrimmed]
        CHECK ([CreditCode] = LTRIM(RTRIM([CreditCode]))),

    CONSTRAINT [CkIncomeTaxCreditsCreditCodeNoSpaces]
        CHECK ([CreditCode] NOT LIKE N'% %'),

    CONSTRAINT [CkIncomeTaxCreditsNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

    CONSTRAINT [CkIncomeTaxCreditsNameTrimmed]
        CHECK ([Name] = LTRIM(RTRIM([Name]))),

    CONSTRAINT [CkIncomeTaxCreditsTaxYearRange]
        CHECK ([TaxYear] BETWEEN 2000 AND 2100),

    CONSTRAINT [CkIncomeTaxCreditsPeriodicity]
        CHECK ([Periodicity] IN (N'Monthly', N'Annual')),

    CONSTRAINT [CkIncomeTaxCreditsAmountNonNegative]
        CHECK ([CreditAmount] >= 0),

    CONSTRAINT [CkIncomeTaxCreditsEffectiveDates]
        CHECK
        (
            [EffectiveToDate] IS NULL
            OR [EffectiveToDate] >= [EffectiveFromDate]
        ),

    CONSTRAINT [CkIncomeTaxCreditsLegalReferenceNotBlank]
        CHECK
        (
            [LegalReference] IS NULL
            OR LEN(LTRIM(RTRIM([LegalReference]))) > 0
        ),

    CONSTRAINT [CkIncomeTaxCreditsLegalReferenceTrimmed]
        CHECK
        (
            [LegalReference] IS NULL
            OR [LegalReference] = LTRIM(RTRIM([LegalReference]))
        )
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxIncomeTaxCreditsCodeYearCurrent]
    ON [Payroll].[IncomeTaxCredits]
    (
        [CreditCode],
        [TaxYear],
        [Periodicity]
    )
    WHERE [EffectiveToDate] IS NULL;
GO
