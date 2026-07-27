CREATE TABLE [Security].[Roles]
(
    [RoleId] int IDENTITY(1,1) NOT NULL,
    [RoleCode] nvarchar(50) NOT NULL,
    [DisplayName] nvarchar(100) NOT NULL,
    [Description] nvarchar(300) NOT NULL,

    [IsSystemRole] bit NOT NULL
        CONSTRAINT [DfRolesIsSystemRole]
        DEFAULT (1),

    [IsActive] bit NOT NULL
        CONSTRAINT [DfRolesIsActive]
        DEFAULT (1),

    [CreatedAtUtc] datetime2(3) NOT NULL
        CONSTRAINT [DfRolesCreatedAtUtc]
        DEFAULT (SYSUTCDATETIME()),

    [UpdatedAtUtc] datetime2(3) NULL,
    [RowVersion] rowversion NOT NULL,

    CONSTRAINT [PkRoles]
        PRIMARY KEY CLUSTERED ([RoleId]),

    CONSTRAINT [UqRolesRoleCode]
        UNIQUE ([RoleCode]),

    CONSTRAINT [CkRolesRoleCodeNotBlank]
        CHECK (LEN(LTRIM(RTRIM([RoleCode]))) > 0),

    CONSTRAINT [CkRolesDisplayNameNotBlank]
        CHECK (LEN(LTRIM(RTRIM([DisplayName]))) > 0),

    CONSTRAINT [CkRolesDescriptionNotBlank]
        CHECK (LEN(LTRIM(RTRIM([Description]))) > 0)
);
GO
