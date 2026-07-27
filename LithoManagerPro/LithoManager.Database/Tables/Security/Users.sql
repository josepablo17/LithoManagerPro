CREATE TABLE [Security].[Users]
(
    [UserId] int IDENTITY(1,1) NOT NULL,
    [RoleId] int NOT NULL,
    [EmailAddress] nvarchar(254) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,

    [IsEmailConfirmed] bit NOT NULL
        CONSTRAINT [DfUsersIsEmailConfirmed]
        DEFAULT (0),

    [IsActive] bit NOT NULL
        CONSTRAINT [DfUsersIsActive]
        DEFAULT (1),

    [RequiresPasswordChange] bit NOT NULL
        CONSTRAINT [DfUsersRequiresPasswordChange]
        DEFAULT (1),

    [TemporaryPasswordExpiresAtUtc] datetime2(3) NULL,
    [PasswordChangedAtUtc] datetime2(3) NULL,

    [FailedLoginAttempts] smallint NOT NULL
        CONSTRAINT [DfUsersFailedLoginAttempts]
        DEFAULT (0),

    [LockoutEndAtUtc] datetime2(3) NULL,
    [LastLoginAtUtc] datetime2(3) NULL,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfUsersCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkUsers]
        PRIMARY KEY CLUSTERED ([UserId]),

    CONSTRAINT [FkUsersRolesRoleId]
        FOREIGN KEY ([RoleId])
        REFERENCES [Security].[Roles] ([RoleId]),

    CONSTRAINT [FkUsersUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkUsersUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqUsersEmailAddress]
        UNIQUE ([EmailAddress]),

    CONSTRAINT [CkUsersEmailAddressNotBlank]
        CHECK (LEN(LTRIM(RTRIM([EmailAddress]))) > 0),

    CONSTRAINT [CkUsersEmailAddressTrimmed]
        CHECK ([EmailAddress] = LTRIM(RTRIM([EmailAddress]))),

    CONSTRAINT [CkUsersEmailAddressNoSpaces]
        CHECK ([EmailAddress] NOT LIKE N'% %'),

    CONSTRAINT [CkUsersPasswordHashNotBlank]
        CHECK (LEN(LTRIM(RTRIM([PasswordHash]))) > 0),

    CONSTRAINT [CkUsersFailedLoginAttempts]
        CHECK ([FailedLoginAttempts] >= 0),

    CONSTRAINT [CkUsersTemporaryPasswordExpiration]
        CHECK
        (
            [TemporaryPasswordExpiresAtUtc] IS NULL
            OR [RequiresPasswordChange] = 1
        )
);
GO

CREATE NONCLUSTERED INDEX [IxUsersRoleIdIsActive]
    ON [Security].[Users]
    (
        [RoleId],
        [IsActive]
    )
    INCLUDE
    (
        [EmailAddress],
        [IsEmailConfirmed],
        [RequiresPasswordChange],
        [LastLoginAtUtc]
    );
GO

CREATE NONCLUSTERED INDEX [IxUsersCreatedByUserId]
    ON [Security].[Users]
    (
        [CreatedByUserId]
    )
    WHERE [CreatedByUserId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IxUsersUpdatedByUserId]
    ON [Security].[Users]
    (
        [UpdatedByUserId]
    )
    WHERE [UpdatedByUserId] IS NOT NULL;
GO
