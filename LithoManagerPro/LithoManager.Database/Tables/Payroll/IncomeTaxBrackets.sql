CREATE TABLE [Payroll].[IncomeTaxBrackets]
(
    [IncomeTaxBracketId] int IDENTITY(1,1) NOT NULL,
    [TaxYear] smallint NOT NULL,
    [Periodicity] nvarchar(20) NOT NULL,
    [LowerBoundAmount] decimal(18,2) NOT NULL,
    [UpperBoundAmount] decimal(18,2) NULL,
    [TaxRate] decimal(9,6) NOT NULL,
    [EffectiveFromDate] date NOT NULL,
    [EffectiveToDate] date NULL,
    [LegalReference] nvarchar(300) NULL,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfIncomeTaxBracketsCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkIncomeTaxBrackets]
        PRIMARY KEY CLUSTERED ([IncomeTaxBracketId]),

    CONSTRAINT [FkIncomeTaxBracketsUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkIncomeTaxBracketsUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqIncomeTaxBracketsYearPeriodicityLowerBound]
        UNIQUE ([TaxYear], [Periodicity], [LowerBoundAmount]),

    CONSTRAINT [CkIncomeTaxBracketsTaxYearRange]
        CHECK ([TaxYear] BETWEEN 2000 AND 2100),

    CONSTRAINT [CkIncomeTaxBracketsPeriodicity]
        CHECK ([Periodicity] IN (N'Monthly', N'Annual')),

    CONSTRAINT [CkIncomeTaxBracketsLowerBoundNonNegative]
        CHECK ([LowerBoundAmount] >= 0),

    CONSTRAINT [CkIncomeTaxBracketsUpperBound]
        CHECK
        (
            [UpperBoundAmount] IS NULL
            OR [UpperBoundAmount] > [LowerBoundAmount]
        ),

    CONSTRAINT [CkIncomeTaxBracketsTaxRateRange]
        CHECK ([TaxRate] >= 0 AND [TaxRate] <= 1),

    CONSTRAINT [CkIncomeTaxBracketsEffectiveDates]
        CHECK
        (
            [EffectiveToDate] IS NULL
            OR [EffectiveToDate] >= [EffectiveFromDate]
        ),

    CONSTRAINT [CkIncomeTaxBracketsLegalReferenceNotBlank]
        CHECK
        (
            [LegalReference] IS NULL
            OR LEN(LTRIM(RTRIM([LegalReference]))) > 0
        ),

    CONSTRAINT [CkIncomeTaxBracketsLegalReferenceTrimmed]
        CHECK
        (
            [LegalReference] IS NULL
            OR [LegalReference] = LTRIM(RTRIM([LegalReference]))
        )
);
GO

CREATE NONCLUSTERED INDEX [IxIncomeTaxBracketsYearPeriodicity]
    ON [Payroll].[IncomeTaxBrackets]
    (
        [TaxYear],
        [Periodicity],
        [LowerBoundAmount]
    )
    INCLUDE
    (
        [UpperBoundAmount],
        [TaxRate],
        [EffectiveFromDate],
        [EffectiveToDate]
    );
GO
