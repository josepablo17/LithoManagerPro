CREATE TABLE [Documents].[EmployeeDocuments]
(
    [EmployeeDocumentId] int IDENTITY(1,1) NOT NULL,
    [EmployeeRecordId] int NOT NULL,
    [DocumentTypeId] int NOT NULL,
    [Title] nvarchar(150) NOT NULL,
    [Description] nvarchar(500) NULL,
    [OriginalFileName] nvarchar(260) NOT NULL,
    [StorageProvider] nvarchar(50) NOT NULL,
    [StorageKey] nvarchar(450) NOT NULL,
    [ContentType] nvarchar(150) NOT NULL,
    [FileSizeBytes] bigint NOT NULL,
    [FileHash] varbinary(32) NOT NULL,
    [FileHashAlgorithm] nvarchar(20) NOT NULL
        CONSTRAINT [DfEmployeeDocumentsFileHashAlgorithm]
        DEFAULT (N'SHA256'),

    [IssuedDate] date NULL,
    [ExpirationDate] date NULL,

    [IsVisibleToEmployee] bit NOT NULL
        CONSTRAINT [DfEmployeeDocumentsIsVisibleToEmployee]
        DEFAULT (0),

    [IsActive] bit NOT NULL
        CONSTRAINT [DfEmployeeDocumentsIsActive]
        DEFAULT (1),

    [DeactivatedAtUtc] datetime2(3) NULL,
    [DeactivatedByUserId] int NULL,

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfEmployeeDocumentsCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NOT NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkEmployeeDocuments]
        PRIMARY KEY CLUSTERED ([EmployeeDocumentId]),

    CONSTRAINT [FkEmployeeDocumentsEmployeeRecordsEmployeeRecordId]
        FOREIGN KEY ([EmployeeRecordId])
        REFERENCES [Documents].[EmployeeRecords] ([EmployeeRecordId]),

    CONSTRAINT [FkEmployeeDocumentsDocumentTypesDocumentTypeId]
        FOREIGN KEY ([DocumentTypeId])
        REFERENCES [Documents].[DocumentTypes] ([DocumentTypeId]),

    CONSTRAINT [FkEmployeeDocumentsUsersDeactivatedByUserId]
        FOREIGN KEY ([DeactivatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkEmployeeDocumentsUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkEmployeeDocumentsUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqEmployeeDocumentsStorageKey]
        UNIQUE ([StorageKey]),

    CONSTRAINT [CkEmployeeDocumentsTitleNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Title]))) > 0),

    CONSTRAINT [CkEmployeeDocumentsTitleTrimmed]
        CHECK ([Title] = LTRIM(RTRIM([Title]))),

    CONSTRAINT [CkEmployeeDocumentsDescriptionNotBlank]
        CHECK
        (
            [Description] IS NULL
            OR LEN(LTRIM(RTRIM([Description]))) > 0
        ),

    CONSTRAINT [CkEmployeeDocumentsDescriptionTrimmed]
        CHECK
        (
            [Description] IS NULL
            OR [Description] = LTRIM(RTRIM([Description]))
        ),

    CONSTRAINT [CkEmployeeDocumentsOriginalFileNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([OriginalFileName]))) > 0),

    CONSTRAINT [CkEmployeeDocumentsOriginalFileNameTrimmed]
        CHECK ([OriginalFileName] = LTRIM(RTRIM([OriginalFileName]))),

    CONSTRAINT [CkEmployeeDocumentsStorageProviderNotBlank]
        CHECK (LEN(LTRIM(RTRIM([StorageProvider]))) > 0),

    CONSTRAINT [CkEmployeeDocumentsStorageProviderTrimmed]
        CHECK ([StorageProvider] = LTRIM(RTRIM([StorageProvider]))),

    CONSTRAINT [CkEmployeeDocumentsStorageKeyNotBlank]
        CHECK (LEN(LTRIM(RTRIM([StorageKey]))) > 0),

    CONSTRAINT [CkEmployeeDocumentsStorageKeyTrimmed]
        CHECK ([StorageKey] = LTRIM(RTRIM([StorageKey]))),

    CONSTRAINT [CkEmployeeDocumentsContentTypeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([ContentType]))) > 0),

    CONSTRAINT [CkEmployeeDocumentsContentTypeTrimmed]
        CHECK ([ContentType] = LTRIM(RTRIM([ContentType]))),

    CONSTRAINT [CkEmployeeDocumentsFileSizePositive]
        CHECK ([FileSizeBytes] > 0),

    CONSTRAINT [CkEmployeeDocumentsFileHashLength]
        CHECK (DATALENGTH([FileHash]) = 32),

    CONSTRAINT [CkEmployeeDocumentsFileHashAlgorithm]
        CHECK ([FileHashAlgorithm] = N'SHA256'),

    CONSTRAINT [CkEmployeeDocumentsExpirationDate]
        CHECK
        (
            [ExpirationDate] IS NULL
            OR [IssuedDate] IS NULL
            OR [ExpirationDate] >= [IssuedDate]
        ),

    CONSTRAINT [CkEmployeeDocumentsActiveState]
        CHECK
        (
            (
                [IsActive] = 1
                AND [DeactivatedAtUtc] IS NULL
                AND [DeactivatedByUserId] IS NULL
            )
            OR
            (
                [IsActive] = 0
                AND [DeactivatedAtUtc] IS NOT NULL
                AND [DeactivatedByUserId] IS NOT NULL
            )
        ),

    CONSTRAINT [CkEmployeeDocumentsDeactivatedAfterCreation]
        CHECK
        (
            [DeactivatedAtUtc] IS NULL
            OR [DeactivatedAtUtc] >= [CreatedAtUtc]
        )
);
GO

CREATE NONCLUSTERED INDEX [IxEmployeeDocumentsRecordActiveCreatedAt]
    ON [Documents].[EmployeeDocuments]
    (
        [EmployeeRecordId],
        [IsActive],
        [CreatedAtUtc] DESC
    )
    INCLUDE
    (
        [DocumentTypeId],
        [Title],
        [OriginalFileName],
        [IsVisibleToEmployee]
    );
GO

CREATE NONCLUSTERED INDEX [IxEmployeeDocumentsRecordVisibleActive]
    ON [Documents].[EmployeeDocuments]
    (
        [EmployeeRecordId],
        [CreatedAtUtc] DESC
    )
    INCLUDE
    (
        [DocumentTypeId],
        [Title],
        [OriginalFileName],
        [ContentType],
        [FileSizeBytes]
    )
    WHERE
        [IsActive] = 1
        AND [IsVisibleToEmployee] = 1;
GO

CREATE NONCLUSTERED INDEX [IxEmployeeDocumentsDocumentTypeActive]
    ON [Documents].[EmployeeDocuments]
    (
        [DocumentTypeId],
        [IsActive],
        [CreatedAtUtc] DESC
    )
    INCLUDE
    (
        [EmployeeRecordId],
        [Title],
        [IsVisibleToEmployee]
    );
GO
