CREATE TABLE [HumanResources].[Employees]
(
    [EmployeeId] int IDENTITY(1,1) NOT NULL,
    [UserId] int NULL,
    [DepartmentId] int NOT NULL,
    [IdentificationNumber] nvarchar(30) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(150) NOT NULL,
    [PhoneNumber] nvarchar(25) NULL,
    [BirthDate] date NULL,
    [HireDate] date NOT NULL,
    [TerminationDate] date NULL,
    [JobTitle] nvarchar(100) NOT NULL,
    [BaseSalary] decimal(18,2) NOT NULL,
    [ProfileImagePath] nvarchar(500) NULL,

    [IsActive] bit NOT NULL
        CONSTRAINT [DfEmployeesIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfEmployeesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [CreatedByUserId] int NULL,
    [UpdatedAtUtc] datetime2(3) NULL,
    [UpdatedByUserId] int NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkEmployees]
        PRIMARY KEY CLUSTERED ([EmployeeId]),

    CONSTRAINT [FkEmployeesUsersUserId]
        FOREIGN KEY ([UserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkEmployeesDepartmentsDepartmentId]
        FOREIGN KEY ([DepartmentId])
        REFERENCES [HumanResources].[Departments] ([DepartmentId]),

    CONSTRAINT [FkEmployeesUsersCreatedByUserId]
        FOREIGN KEY ([CreatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [FkEmployeesUsersUpdatedByUserId]
        FOREIGN KEY ([UpdatedByUserId])
        REFERENCES [Security].[Users] ([UserId]),

    CONSTRAINT [UqEmployeesIdentificationNumber]
        UNIQUE ([IdentificationNumber]),

    CONSTRAINT [CkEmployeesIdentificationNumberNotBlank]
        CHECK (LEN(LTRIM(RTRIM([IdentificationNumber]))) > 0),

    CONSTRAINT [CkEmployeesIdentificationNumberTrimmed]
        CHECK ([IdentificationNumber] = LTRIM(RTRIM([IdentificationNumber]))),

    CONSTRAINT [CkEmployeesFirstNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([FirstName]))) > 0),

    CONSTRAINT [CkEmployeesFirstNameTrimmed]
        CHECK ([FirstName] = LTRIM(RTRIM([FirstName]))),

    CONSTRAINT [CkEmployeesLastNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([LastName]))) > 0),

    CONSTRAINT [CkEmployeesLastNameTrimmed]
        CHECK ([LastName] = LTRIM(RTRIM([LastName]))),

    CONSTRAINT [CkEmployeesJobTitleNotBlank]
        CHECK (LEN(LTRIM(RTRIM([JobTitle]))) > 0),

    CONSTRAINT [CkEmployeesJobTitleTrimmed]
        CHECK ([JobTitle] = LTRIM(RTRIM([JobTitle]))),

    CONSTRAINT [CkEmployeesPhoneNumberNotBlank]
        CHECK
        (
            [PhoneNumber] IS NULL
            OR LEN(LTRIM(RTRIM([PhoneNumber]))) > 0
        ),

    CONSTRAINT [CkEmployeesProfileImagePathNotBlank]
        CHECK
        (
            [ProfileImagePath] IS NULL
            OR LEN(LTRIM(RTRIM([ProfileImagePath]))) > 0
        ),

    CONSTRAINT [CkEmployeesBaseSalaryNonNegative]
        CHECK ([BaseSalary] >= 0),

    CONSTRAINT [CkEmployeesEmploymentDates]
        CHECK
        (
            [TerminationDate] IS NULL
            OR [TerminationDate] >= [HireDate]
        )
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UxEmployeesUserId]
    ON [HumanResources].[Employees]
    (
        [UserId]
    )
    WHERE [UserId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IxEmployeesDepartmentIdIsActive]
    ON [HumanResources].[Employees]
    (
        [DepartmentId],
        [IsActive]
    )
    INCLUDE
    (
        [UserId],
        [IdentificationNumber],
        [FirstName],
        [LastName],
        [JobTitle]
    );
GO
