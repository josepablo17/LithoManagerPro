CREATE TABLE [Payroll].[SocialContributionRates]
(
    [SocialContributionRateId] int IDENTITY(1,1) NOT NULL,
    [SocialContributionTypeId] int NOT NULL,
    [EmployeeRate] decimal(9,6) NOT NULL,
    [EmployerRate] decimal(9,6) NOT NULL,
    [EffectiveFromDate] date NOT NULL,
    [EffectiveToDate] date NULL,
    [LegalReference] nvarchar(300) NULL,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfSocialContributionRatesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkSocialContributionRates]
        PRIMARY KEY CLUSTERED ([SocialContributionRateId]),

    CONSTRAINT [FkSocialContributionRatesSocialContributionTypesSocialContributionTypeId]
        FOREIGN KEY ([SocialContributionTypeId])
        REFERENCES [Payroll].[SocialContributionTypes] ([SocialContributionTypeId]),

    CONSTRAINT [FkSocialContributionRatesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkSocialContributionRatesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqSocialContributionRatesTypeEffectiveFrom]
        UNIQUE ([SocialContributionTypeId], [EffectiveFromDate]),

    CONSTRAINT [CkSocialContributionRatesEmployeeRateRange]
        CHECK ([EmployeeRate] >= 0 AND [EmployeeRate] <= 1),

    CONSTRAINT [CkSocialContributionRatesEmployerRateRange]
        CHECK ([EmployerRate] >= 0 AND [EmployerRate] <= 1),

    CONSTRAINT [CkSocialContributionRatesAtLeastOneRate]
        CHECK ([EmployeeRate] > 0 OR [EmployerRate] > 0),

    CONSTRAINT [CkSocialContributionRatesEffectiveDates]
        CHECK
        (
            [EffectiveToDate] IS NULL
            OR [EffectiveToDate] >= [EffectiveFromDate]
        ),

    CONSTRAINT [CkSocialContributionRatesLegalReferenceNotBlank]
        CHECK
        (
            [LegalReference] IS NULL
            OR LEN(LTRIM(RTRIM([LegalReference]))) > 0
        ),

    CONSTRAINT [CkSocialContributionRatesLegalReferenceTrimmed]
        CHECK
        (
            [LegalReference] IS NULL
            OR [LegalReference] = LTRIM(RTRIM([LegalReference]))
        )
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxSocialContributionRatesTypeCurrent]
    ON [Payroll].[SocialContributionRates]
    (
        [SocialContributionTypeId]
    )
    WHERE [EffectiveToDate] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IxSocialContributionRatesTypeEffectiveDates]
    ON [Payroll].[SocialContributionRates]
    (
        [SocialContributionTypeId],
        [EffectiveFromDate] DESC
    )
    INCLUDE
    (
        [EffectiveToDate],
        [EmployeeRate],
        [EmployerRate]
    );
GO
