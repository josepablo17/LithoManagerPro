CREATE TABLE [Audit].[AuditLogs]
(
    [AuditLogId] bigint IDENTITY(1,1) NOT NULL,

    [CorrelationId] uniqueidentifier NOT NULL
        CONSTRAINT [DfAuditLogsCorrelationId]
        DEFAULT (NEWID()),

    [ModuleName] nvarchar(128) NOT NULL,
    [ActionName] nvarchar(100) NOT NULL,
    [EntityName] nvarchar(128) NULL,
    [EntityId] nvarchar(100) NULL,
    [ActorType] nvarchar(20) NOT NULL,
    [ActorUserId] int NULL,
    [ActorEmailAddress] nvarchar(254) NULL,
    [ActorRoleCode] nvarchar(50) NULL,

    [IsSuccessful] bit NOT NULL
        CONSTRAINT [DfAuditLogsIsSuccessful]
        DEFAULT (1),

    [EventDescription] nvarchar(500) NULL,
    [ClientIpAddress] nvarchar(45) NULL,
    [UserAgent] nvarchar(512) NULL,
    [HttpMethod] nvarchar(10) NULL,
    [RequestPath] nvarchar(500) NULL,
    [PreviousValuesJson] nvarchar(max) NULL,
    [NewValuesJson] nvarchar(max) NULL,
    [AdditionalDataJson] nvarchar(max) NULL,
    [ErrorMessage] nvarchar(2000) NULL,

    [OccurredAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfAuditLogsOccurredAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PkAuditLogs]
        PRIMARY KEY CLUSTERED ([AuditLogId]),

    CONSTRAINT [CkAuditLogsModuleNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([ModuleName]))) > 0),

    CONSTRAINT [CkAuditLogsActionNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([ActionName]))) > 0),

    CONSTRAINT [CkAuditLogsActorType]
        CHECK ([ActorType] IN (N'User', N'System', N'Anonymous')),

    CONSTRAINT [CkAuditLogsUserActor]
        CHECK ([ActorType] <> N'User' OR [ActorUserId] IS NOT NULL),

    CONSTRAINT [CkAuditLogsHttpMethod]
        CHECK
        (
            [HttpMethod] IS NULL
            OR [HttpMethod] IN (N'GET', N'POST', N'PUT', N'PATCH', N'DELETE')
        ),

    CONSTRAINT [CkAuditLogsPreviousValuesJson]
        CHECK ([PreviousValuesJson] IS NULL OR ISJSON([PreviousValuesJson]) = 1),

    CONSTRAINT [CkAuditLogsNewValuesJson]
        CHECK ([NewValuesJson] IS NULL OR ISJSON([NewValuesJson]) = 1),

    CONSTRAINT [CkAuditLogsAdditionalDataJson]
        CHECK ([AdditionalDataJson] IS NULL OR ISJSON([AdditionalDataJson]) = 1)
);
GO

CREATE NONCLUSTERED INDEX [IxAuditLogsOccurredAtUtc]
    ON [Audit].[AuditLogs]
    (
        [OccurredAtUtc] DESC
    );
GO

CREATE NONCLUSTERED INDEX [IxAuditLogsActorUserIdOccurredAtUtc]
    ON [Audit].[AuditLogs]
    (
        [ActorUserId],
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [ModuleName],
        [ActionName],
        [EntityName],
        [EntityId],
        [IsSuccessful]
    )
    WHERE [ActorUserId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IxAuditLogsEntityNameEntityIdOccurredAtUtc]
    ON [Audit].[AuditLogs]
    (
        [EntityName],
        [EntityId],
        [OccurredAtUtc] DESC
    )
    INCLUDE
    (
        [ModuleName],
        [ActionName],
        [ActorUserId],
        [IsSuccessful]
    )
    WHERE [EntityName] IS NOT NULL
      AND [EntityId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IxAuditLogsCorrelationId]
    ON [Audit].[AuditLogs]
    (
        [CorrelationId]
    );
GO
