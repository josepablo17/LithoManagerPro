CREATE TABLE [Documents].[DocumentTypes]
(
    [DocumentTypeId] int IDENTITY(1,1) NOT NULL,
    [DocumentTypeCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(250) NULL,

    [DefaultIsVisibleToEmployee] bit NOT NULL
        CONSTRAINT [DfDocumentTypesDefaultIsVisibleToEmployee]
        DEFAULT (0),

    [IsActive] bit NOT NULL
        CONSTRAINT [DfDocumentTypesIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfDocumentTypesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkDocumentTypes]
        PRIMARY KEY CLUSTERED ([DocumentTypeId]),

    CONSTRAINT [FkDocumentTypesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkDocumentTypesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqDocumentTypesDocumentTypeCode]
        UNIQUE ([DocumentTypeCode]),

    CONSTRAINT [UqDocumentTypesName]
        UNIQUE ([Name]),

    CONSTRAINT [CkDocumentTypesDocumentTypeCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([DocumentTypeCode]))) > 0),

    CONSTRAINT [CkDocumentTypesDocumentTypeCodeTrimmed]
        CHECK ([DocumentTypeCode] = LTRIM(RTRIM([DocumentTypeCode]))),

    CONSTRAINT [CkDocumentTypesDocumentTypeCodeNoSpaces]
        CHECK ([DocumentTypeCode] NOT LIKE N'% %'),

    CONSTRAINT [CkDocumentTypesNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

    CONSTRAINT [CkDocumentTypesNameTrimmed]
        CHECK ([Name] = LTRIM(RTRIM([Name]))),

    CONSTRAINT [CkDocumentTypesDescriptionNotBlank]
        CHECK
        (
            [Description] IS NULL
            OR LEN(LTRIM(RTRIM([Description]))) > 0
        ),

    CONSTRAINT [CkDocumentTypesDescriptionTrimmed]
        CHECK
        (
            [Description] IS NULL
            OR [Description] = LTRIM(RTRIM([Description]))
        )
);
GO

CREATE NONCLUSTERED INDEX [IxDocumentTypesIsActive]
    ON [Documents].[DocumentTypes]
    (
        [IsActive]
    )
    INCLUDE
    (
        [DocumentTypeCode],
        [Name],
        [DefaultIsVisibleToEmployee]
    );
GO
