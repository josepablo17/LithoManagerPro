CREATE TABLE [Security].[PasswordResetTokens]
(
    [PasswordResetTokenId] int IDENTITY(1, 1) NOT NULL,
    [UserId] int NOT NULL,
    [TokenHash] varbinary(32) NOT NULL,
    [ExpiresAtUtc] datetime2(3) NOT NULL,
    [UsedAtUtc] datetime2(3) NULL,
    [RevokedAtUtc] datetime2(3) NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfPasswordResetTokensCreatedAtUtc]
        DEFAULT SYSUTCDATETIME(),
    [CorrelationId] uniqueidentifier NOT NULL,

    CONSTRAINT [PkPasswordResetTokens]
        PRIMARY KEY CLUSTERED
        (
            [PasswordResetTokenId] ASC
        ),

    CONSTRAINT [FkPasswordResetTokensUsersUserId]
        FOREIGN KEY ([UserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqPasswordResetTokensTokenHash]
        UNIQUE NONCLUSTERED
        (
            [TokenHash] ASC
        ),

    CONSTRAINT [CkPasswordResetTokensTokenHashLength]
        CHECK (DATALENGTH([TokenHash]) = 32),

    CONSTRAINT [CkPasswordResetTokensExpiresAfterCreation]
        CHECK ([ExpiresAtUtc] > [CreatedAtUtc]),

    CONSTRAINT [CkPasswordResetTokensUsedAfterCreation]
        CHECK
        (
            [UsedAtUtc] IS NULL
            OR [UsedAtUtc] >= [CreatedAtUtc]
        ),

    CONSTRAINT [CkPasswordResetTokensUsedBeforeExpiration]
        CHECK
        (
            [UsedAtUtc] IS NULL
            OR [UsedAtUtc] <= [ExpiresAtUtc]
        ),

    CONSTRAINT [CkPasswordResetTokensRevokedAfterCreation]
        CHECK
        (
            [RevokedAtUtc] IS NULL
            OR [RevokedAtUtc] >= [CreatedAtUtc]
        ),

    CONSTRAINT [CkPasswordResetTokensSingleTerminalState]
        CHECK
        (
            [UsedAtUtc] IS NULL
            OR [RevokedAtUtc] IS NULL
        ),

    CONSTRAINT [CkPasswordResetTokensCorrelationIdNotEmpty]
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

CREATE NONCLUSTERED INDEX [IxPasswordResetTokensUserIdCreatedAtUtc]
    ON [Security].[PasswordResetTokens]
    (
        [UserId] ASC,
        [CreatedAtUtc] DESC
    )
    INCLUDE
    (
        [PasswordResetTokenId],
        [ExpiresAtUtc],
        [UsedAtUtc],
        [RevokedAtUtc],
        [CorrelationId]
    );
GO

CREATE NONCLUSTERED INDEX [IxPasswordResetTokensUserIdActive]
    ON [Security].[PasswordResetTokens]
    (
        [UserId] ASC,
        [CreatedAtUtc] DESC
    )
    INCLUDE
    (
        [PasswordResetTokenId],
        [ExpiresAtUtc],
        [CorrelationId]
    )
    WHERE
        [UsedAtUtc] IS NULL
        AND [RevokedAtUtc] IS NULL;
GO