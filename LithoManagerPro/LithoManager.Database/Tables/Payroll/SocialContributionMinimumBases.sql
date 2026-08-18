CREATE TABLE [Payroll].[SocialContributionMinimumBases]
(
    [SocialContributionMinimumBaseId] int IDENTITY(1,1) NOT NULL,
    [SocialContributionTypeId] int NOT NULL,
    [MinimumBaseAmount] decimal(18,2) NOT NULL,
    [EffectiveFromDate] date NOT NULL,
    [EffectiveToDate] date NULL,
    [LegalReference] nvarchar(300) NULL,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfSocialContributionMinimumBasesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkSocialContributionMinimumBases]
        PRIMARY KEY CLUSTERED ([SocialContributionMinimumBaseId]),

    CONSTRAINT [FkSocialContributionMinimumBasesSocialContributionTypesSocialContributionTypeId]
        FOREIGN KEY ([SocialContributionTypeId])
        REFERENCES [Payroll].[SocialContributionTypes] ([SocialContributionTypeId]),

    CONSTRAINT [FkSocialContributionMinimumBasesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkSocialContributionMinimumBasesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqSocialContributionMinimumBasesTypeEffectiveFrom]
        UNIQUE ([SocialContributionTypeId], [EffectiveFromDate]),

    CONSTRAINT [CkSocialContributionMinimumBasesAmountPositive]
        CHECK ([MinimumBaseAmount] > 0),

    CONSTRAINT [CkSocialContributionMinimumBasesEffectiveDates]
        CHECK
        (
            [EffectiveToDate] IS NULL
            OR [EffectiveToDate] >= [EffectiveFromDate]
        ),

    CONSTRAINT [CkSocialContributionMinimumBasesLegalReferenceNotBlank]
        CHECK
        (
            [LegalReference] IS NULL
            OR LEN(LTRIM(RTRIM([LegalReference]))) > 0
        ),

    CONSTRAINT [CkSocialContributionMinimumBasesLegalReferenceTrimmed]
        CHECK
        (
            [LegalReference] IS NULL
            OR [LegalReference] = LTRIM(RTRIM([LegalReference]))
        )
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxSocialContributionMinimumBasesTypeCurrent]
    ON [Payroll].[SocialContributionMinimumBases]
    (
        [SocialContributionTypeId]
    )
    WHERE [EffectiveToDate] IS NULL;
GO
