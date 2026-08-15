SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DocumentTypes TABLE
(
    DocumentTypeCode nvarchar(50) NOT NULL PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Description nvarchar(250) NULL,
    DefaultIsVisibleToEmployee bit NOT NULL,
    IsActive bit NOT NULL
);

INSERT INTO @DocumentTypes
(
    DocumentTypeCode,
    Name,
    Description,
    DefaultIsVisibleToEmployee,
    IsActive
)
VALUES
(
    N'EmploymentContract',
    N'Employment Contract',
    N'Employee employment contract.',
    1,
    1
),
(
    N'InsuranceDocument',
    N'Insurance Document',
    N'Employee insurance or policy documentation.',
    1,
    1
),
(
    N'PersonalDataDocument',
    N'Personal Data Document',
    N'Document containing employee personal information.',
    1,
    1
),
(
    N'Other',
    N'Other',
    N'Additional employee document.',
    0,
    1
);

UPDATE TargetDocumentType
SET
    TargetDocumentType.Name = SourceDocumentType.Name,
    TargetDocumentType.Description = SourceDocumentType.Description,
    TargetDocumentType.DefaultIsVisibleToEmployee =
        SourceDocumentType.DefaultIsVisibleToEmployee,
    TargetDocumentType.IsActive = SourceDocumentType.IsActive,
    TargetDocumentType.UpdatedAtUtc = SYSUTCDATETIME()
FROM Documents.DocumentTypes AS TargetDocumentType
INNER JOIN @DocumentTypes AS SourceDocumentType
    ON SourceDocumentType.DocumentTypeCode =
        TargetDocumentType.DocumentTypeCode
WHERE
    TargetDocumentType.Name <> SourceDocumentType.Name
    OR ISNULL(TargetDocumentType.Description, N'') <>
        ISNULL(SourceDocumentType.Description, N'')
    OR TargetDocumentType.DefaultIsVisibleToEmployee <>
        SourceDocumentType.DefaultIsVisibleToEmployee
    OR TargetDocumentType.IsActive <> SourceDocumentType.IsActive;

INSERT INTO Documents.DocumentTypes
(
    DocumentTypeCode,
    Name,
    Description,
    DefaultIsVisibleToEmployee,
    IsActive
)
SELECT
    SourceDocumentType.DocumentTypeCode,
    SourceDocumentType.Name,
    SourceDocumentType.Description,
    SourceDocumentType.DefaultIsVisibleToEmployee,
    SourceDocumentType.IsActive
FROM @DocumentTypes AS SourceDocumentType
WHERE NOT EXISTS
(
    SELECT 1
    FROM Documents.DocumentTypes AS ExistingDocumentType
    WHERE ExistingDocumentType.DocumentTypeCode =
        SourceDocumentType.DocumentTypeCode
);
