SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
    Seed: Security.Roles

    RoleCode is the permanent technical identifier.
    Never use RoleId as a fixed value in application code.
*/

DECLARE @Roles TABLE
(
    RoleCode nvarchar(50) NOT NULL PRIMARY KEY,
    DisplayName nvarchar(100) NOT NULL,
    Description nvarchar(300) NOT NULL
);

INSERT INTO @Roles
(
    RoleCode,
    DisplayName,
    Description
)
VALUES
(
    N'SuperAdministrator',
    N'Super Administrator',
    N'Has unrestricted access to security, configuration and all system modules.'
),
(
    N'HumanResourcesAdministrator',
    N'Human Resources Administrator',
    N'Manages human resources processes, employees and regular user accounts.'
),
(
    N'HumanResourcesStaff',
    N'Human Resources Staff',
    N'Performs operational human resources activities without full security administration privileges.'
),
(
    N'Employee',
    N'Employee',
    N'Uses employee self-service functions and accesses only authorized personal information.'
);

/*
    Update descriptive information if the role already exists.
    IsActive is intentionally preserved.
*/

UPDATE TargetRole
SET
    TargetRole.DisplayName = SourceRole.DisplayName,
    TargetRole.Description = SourceRole.Description,
    TargetRole.IsSystemRole = 1,
    TargetRole.UpdatedAtUtc = SYSUTCDATETIME()
FROM Security.Roles AS TargetRole
INNER JOIN @Roles AS SourceRole
    ON SourceRole.RoleCode = TargetRole.RoleCode
WHERE
    TargetRole.DisplayName <> SourceRole.DisplayName
    OR TargetRole.Description <> SourceRole.Description
    OR TargetRole.IsSystemRole <> 1;

/*
    Insert only roles that do not exist.
*/

INSERT INTO Security.Roles
(
    RoleCode,
    DisplayName,
    Description,
    IsSystemRole,
    IsActive
)
SELECT
    SourceRole.RoleCode,
    SourceRole.DisplayName,
    SourceRole.Description,
    1,
    1
FROM @Roles AS SourceRole
WHERE NOT EXISTS
(
    SELECT 1
    FROM Security.Roles AS ExistingRole
    WHERE ExistingRole.RoleCode = SourceRole.RoleCode
);