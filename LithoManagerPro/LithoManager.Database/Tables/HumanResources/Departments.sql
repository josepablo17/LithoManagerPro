CREATE TABLE [HumanResources].[Departments]
(
    [DepartmentId] int IDENTITY(1,1) NOT NULL,
    [DepartmentCode] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(300) NULL,

    [IsActive] bit NOT NULL
        CONSTRAINT [DfDepartmentsIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfDepartmentsCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkDepartments]
        PRIMARY KEY CLUSTERED ([DepartmentId]),

    CONSTRAINT [FkDepartmentsUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkDepartmentsUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqDepartmentsDepartmentCode]
        UNIQUE ([DepartmentCode]),

    CONSTRAINT [UqDepartmentsName]
        UNIQUE ([Name]),

    CONSTRAINT [CkDepartmentsDepartmentCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([DepartmentCode]))) > 0),

    CONSTRAINT [CkDepartmentsDepartmentCodeTrimmed]
        CHECK ([DepartmentCode] = LTRIM(RTRIM([DepartmentCode]))),

    CONSTRAINT [CkDepartmentsDepartmentCodeNoSpaces]
        CHECK ([DepartmentCode] NOT LIKE N'% %'),

    CONSTRAINT [CkDepartmentsNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

    CONSTRAINT [CkDepartmentsNameTrimmed]
        CHECK ([Name] = LTRIM(RTRIM([Name]))),

    CONSTRAINT [CkDepartmentsDescriptionNotBlank]
        CHECK
        (
            [Description] IS NULL
            OR LEN(LTRIM(RTRIM([Description]))) > 0
        )
);
GO
