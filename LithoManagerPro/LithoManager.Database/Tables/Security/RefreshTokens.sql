CREATE TABLE [Security].[RefreshTokens]
(
    [RefreshTokenId] int IDENTITY(1, 1) NOT NULL,
    [UserId] int NOT NULL,
    [TokenHash] varbinary(32) NOT NULL,
    [TokenFamilyId] uniqueidentifier NOT NULL,
    [TokenVersion] int NOT NULL,
    [ReplacedByRefreshTokenId] int NULL,
    [ReplacedAtUtc] datetime2(3) NULL,
    [ExpiresAtUtc] datetime2(3) NOT NULL,
    [RevokedAtUtc] datetime2(3) NULL,
    [RevokedReason] nvarchar(100) NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfRefreshTokensCreatedAtUtc]
        DEFAULT SYSUTCDATETIME(),
    [CreatedByIpAddress] nvarchar(45) NULL,
    [CreatedByUserAgent] nvarchar(512) NULL,
    [LastUsedAtUtc] datetime2(3) NULL,
    [LastUsedByIpAddress] nvarchar(45) NULL,
    [LastUsedByUserAgent] nvarchar(512) NULL,
    [CorrelationId] uniqueidentifier NOT NULL,

    CONSTRAINT [PkRefreshTokens]
        PRIMARY KEY CLUSTERED
        (
            [RefreshTokenId] ASC
        ),

    CONSTRAINT [FkRefreshTokensUsersUserId]
        FOREIGN KEY ([UserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkRefreshTokensRefreshTokensReplacedByRefreshTokenId]
        FOREIGN KEY ([ReplacedByRefreshTokenId])
        REFERENCES [Security].[RefreshTokens] ([RefreshTokenId]),

    CONSTRAINT [UqRefreshTokensTokenHash]
        UNIQUE NONCLUSTERED
        (
            [TokenHash] ASC
        ),

    CONSTRAINT [CkRefreshTokensTokenHashLength]
        CHECK (DATALENGTH([TokenHash]) = 32),

    CONSTRAINT [CkRefreshTokensTokenFamilyIdNotEmpty]
        CHECK
        (
            [TokenFamilyId]
            <> CONVERT(
                uniqueidentifier,
                '00000000-0000-0000-0000-000000000000'
            )
        ),

    CONSTRAINT [CkRefreshTokensTokenVersion]
        CHECK ([TokenVersion] >= 1),

    CONSTRAINT [CkRefreshTokensExpiresAfterCreation]
        CHECK ([ExpiresAtUtc] > [CreatedAtUtc]),

    CONSTRAINT [CkRefreshTokensReplacedByDifferentToken]
        CHECK
        (
            [ReplacedByRefreshTokenId] IS NULL
            OR [ReplacedByRefreshTokenId] <> [RefreshTokenId]
        ),

    CONSTRAINT [CkRefreshTokensReplacedAfterCreation]
        CHECK
        (
            [ReplacedAtUtc] IS NULL
            OR [ReplacedAtUtc] >= [CreatedAtUtc]
        ),

    CONSTRAINT [CkRefreshTokensReplacedByRequiresReplacedAt]
        CHECK
        (
            [ReplacedByRefreshTokenId] IS NULL
            OR [ReplacedAtUtc] IS NOT NULL
        ),

    CONSTRAINT [CkRefreshTokensRevokedAfterCreation]
        CHECK
        (
            [RevokedAtUtc] IS NULL
            OR [RevokedAtUtc] >= [CreatedAtUtc]
        ),

    CONSTRAINT [CkRefreshTokensRevokedReasonNotBlank]
        CHECK
        (
            [RevokedReason] IS NULL
            OR LEN(LTRIM(RTRIM([RevokedReason]))) > 0
        ),

    CONSTRAINT [CkRefreshTokensSingleTerminalState]
        CHECK
        (
            [ReplacedAtUtc] IS NULL
            OR [RevokedAtUtc] IS NULL
        ),

    CONSTRAINT [CkRefreshTokensLastUsedAfterCreation]
        CHECK
        (
            [LastUsedAtUtc] IS NULL
            OR [LastUsedAtUtc] >= [CreatedAtUtc]
        ),

    CONSTRAINT [CkRefreshTokensLastUsedBeforeExpiration]
        CHECK
        (
            [LastUsedAtUtc] IS NULL
            OR [LastUsedAtUtc] <= [ExpiresAtUtc]
        ),

    CONSTRAINT [CkRefreshTokensCorrelationIdNotEmpty]
        CHECK
        (
            [CorrelationId]
            <> CONVERT(
                uniqueidentifier,
                '00000000-0000-0000-0000-000000000000'
            )
        )
);
GO

CREATE NONCLUSTERED INDEX [IxRefreshTokensUserIdCreatedAtUtc]
    ON [Security].[RefreshTokens]
    (
        [UserId] ASC,
        [CreatedAtUtc] DESC
    )
    INCLUDE
    (
        [RefreshTokenId],
        [TokenFamilyId],
        [TokenVersion],
        [ExpiresAtUtc],
        [ReplacedByRefreshTokenId],
        [ReplacedAtUtc],
        [RevokedAtUtc],
        [CorrelationId]
    );
GO

CREATE NONCLUSTERED INDEX [IxRefreshTokensTokenFamilyId]
    ON [Security].[RefreshTokens]
    (
        [TokenFamilyId] ASC,
        [CreatedAtUtc] DESC
    )
    INCLUDE
    (
        [RefreshTokenId],
        [UserId],
        [TokenVersion],
        [ExpiresAtUtc],
        [ReplacedByRefreshTokenId],
        [ReplacedAtUtc],
        [RevokedAtUtc]
    );
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxRefreshTokensUserIdActive]
    ON [Security].[RefreshTokens]
    (
        [UserId] ASC
    )
    INCLUDE
    (
        [RefreshTokenId],
        [TokenFamilyId],
        [TokenVersion],
        [ExpiresAtUtc],
        [CorrelationId]
    )
    WHERE
        [ReplacedAtUtc] IS NULL
        AND [RevokedAtUtc] IS NULL;
GO
