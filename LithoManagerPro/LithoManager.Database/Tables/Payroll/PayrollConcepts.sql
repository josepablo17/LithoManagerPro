CREATE TABLE [Payroll].[PayrollConcepts]
(
    [PayrollConceptId] int IDENTITY(1,1) NOT NULL,
    [PayrollConceptCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(300) NOT NULL,
    [ConceptKind] nvarchar(30) NOT NULL,

    [IsSystemConcept] bit NOT NULL
        CONSTRAINT [DfPayrollConceptsIsSystemConcept]
        DEFAULT (1),

    [IsTaxableForIncomeTax] bit NOT NULL
        CONSTRAINT [DfPayrollConceptsIsTaxableForIncomeTax]
        DEFAULT (0),

    [IsSubjectToSocialContributions] bit NOT NULL
        CONSTRAINT [DfPayrollConceptsIsSubjectToSocialContributions]
        DEFAULT (0),

    [CountsForAguinaldo] bit NOT NULL
        CONSTRAINT [DfPayrollConceptsCountsForAguinaldo]
        DEFAULT (0),

    [IsActive] bit NOT NULL
        CONSTRAINT [DfPayrollConceptsIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfPayrollConceptsCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkPayrollConcepts]
        PRIMARY KEY CLUSTERED ([PayrollConceptId]),

    CONSTRAINT [FkPayrollConceptsUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkPayrollConceptsUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqPayrollConceptsPayrollConceptCode]
        UNIQUE ([PayrollConceptCode]),

    CONSTRAINT [UqPayrollConceptsName]
        UNIQUE ([Name]),

    CONSTRAINT [CkPayrollConceptsPayrollConceptCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([PayrollConceptCode]))) > 0),

    CONSTRAINT [CkPayrollConceptsPayrollConceptCodeTrimmed]
        CHECK ([PayrollConceptCode] = LTRIM(RTRIM([PayrollConceptCode]))),

    CONSTRAINT [CkPayrollConceptsPayrollConceptCodeNoSpaces]
        CHECK ([PayrollConceptCode] NOT LIKE N'% %'),

    CONSTRAINT [CkPayrollConceptsNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

    CONSTRAINT [CkPayrollConceptsNameTrimmed]
        CHECK ([Name] = LTRIM(RTRIM([Name]))),

    CONSTRAINT [CkPayrollConceptsDescriptionNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Description]))) > 0),

    CONSTRAINT [CkPayrollConceptsDescriptionTrimmed]
        CHECK ([Description] = LTRIM(RTRIM([Description]))),

    CONSTRAINT [CkPayrollConceptsConceptKind]
        CHECK
        (
            [ConceptKind] IN
            (
                N'Earning',
                N'Deduction',
                N'EmployerContribution',
                N'Informational'
            )
        )
);
GO

CREATE NONCLUSTERED INDEX [IxPayrollConceptsIsActiveConceptKind]
    ON [Payroll].[PayrollConcepts]
    (
        [IsActive],
        [ConceptKind]
    )
    INCLUDE
    (
        [PayrollConceptCode],
        [Name],
        [IsTaxableForIncomeTax],
        [IsSubjectToSocialContributions],
        [CountsForAguinaldo]
    );
GO
