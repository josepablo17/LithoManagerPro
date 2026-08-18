CREATE TABLE [Payroll].[SocialContributionTypes]
(
    [SocialContributionTypeId] int IDENTITY(1,1) NOT NULL,
    [ContributionCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [InstitutionName] nvarchar(100) NOT NULL,
    [ContributionGroup] nvarchar(30) NOT NULL,

    [AppliesToEmployee] bit NOT NULL
        CONSTRAINT [DfSocialContributionTypesAppliesToEmployee]
        DEFAULT (0),

    [AppliesToEmployer] bit NOT NULL
        CONSTRAINT [DfSocialContributionTypesAppliesToEmployer]
        DEFAULT (0),

    [UsesMinimumBase] bit NOT NULL
        CONSTRAINT [DfSocialContributionTypesUsesMinimumBase]
        DEFAULT (0),

    [IsActive] bit NOT NULL
        CONSTRAINT [DfSocialContributionTypesIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfSocialContributionTypesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkSocialContributionTypes]
        PRIMARY KEY CLUSTERED ([SocialContributionTypeId]),

    CONSTRAINT [FkSocialContributionTypesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkSocialContributionTypesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqSocialContributionTypesContributionCode]
        UNIQUE ([ContributionCode]),

    CONSTRAINT [UqSocialContributionTypesName]
        UNIQUE ([Name]),

    CONSTRAINT [CkSocialContributionTypesContributionCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([ContributionCode]))) > 0),

    CONSTRAINT [CkSocialContributionTypesContributionCodeTrimmed]
        CHECK ([ContributionCode] = LTRIM(RTRIM([ContributionCode]))),

    CONSTRAINT [CkSocialContributionTypesContributionCodeNoSpaces]
        CHECK ([ContributionCode] NOT LIKE N'% %'),

    CONSTRAINT [CkSocialContributionTypesNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

    CONSTRAINT [CkSocialContributionTypesNameTrimmed]
        CHECK ([Name] = LTRIM(RTRIM([Name]))),

    CONSTRAINT [CkSocialContributionTypesInstitutionNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([InstitutionName]))) > 0),

    CONSTRAINT [CkSocialContributionTypesInstitutionNameTrimmed]
        CHECK ([InstitutionName] = LTRIM(RTRIM([InstitutionName]))),

    CONSTRAINT [CkSocialContributionTypesContributionGroup]
        CHECK
        (
            [ContributionGroup] IN
            (
                N'CCSS',
                N'OtherInstitution',
                N'LPT'
            )
        ),

    CONSTRAINT [CkSocialContributionTypesAppliesToAtLeastOneSide]
        CHECK
        (
            [AppliesToEmployee] = 1
            OR [AppliesToEmployer] = 1
        )
);
GO

CREATE NONCLUSTERED INDEX [IxSocialContributionTypesIsActiveContributionGroup]
    ON [Payroll].[SocialContributionTypes]
    (
        [IsActive],
        [ContributionGroup]
    )
    INCLUDE
    (
        [ContributionCode],
        [Name],
        [AppliesToEmployee],
        [AppliesToEmployer],
        [UsesMinimumBase]
    );
GO
