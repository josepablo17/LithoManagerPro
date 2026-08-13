SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
    Manual test data for LeaveManagement.

    This script is intentionally not referenced by PostDeployment.sql.
    Execute it only in development or test databases when manual UI testing
    needs known users.

    Password for both accounts:
    LithoTest2026!

    Accounts:
    - empleado.vacaciones@lithomanager.local
    - rrhh.vacaciones@lithomanager.local
*/

DECLARE @EmployeeEmailAddress nvarchar(254) =
    N'empleado.vacaciones@lithomanager.local';

DECLARE @HrEmailAddress nvarchar(254) =
    N'rrhh.vacaciones@lithomanager.local';

DECLARE @EmployeePasswordHash nvarchar(500) =
    N'AQAAAAEAACcQAAAAEIe/hteDctRjW9exlezFqPCfVbU2kPE6VSrjjNUnEx/nhjW5CRSaprvOxCqMFqJh/w==';

DECLARE @HrPasswordHash nvarchar(500) =
    N'AQAAAAEAACcQAAAAEPi9KZF4bSY94zQWqJt8lxdKkCM26zb47QyG87ToxqcN0aIOaCSylLDrplyoEs3ysw==';

DECLARE @DepartmentCode nvarchar(50) =
    N'QALEAVE';

DECLARE @DepartmentName nvarchar(100) =
    N'Pruebas Vacaciones';

DECLARE @EmployeeIdentificationNumber nvarchar(30) =
    N'LM-LEAVE-EMP';

DECLARE @HrIdentificationNumber nvarchar(30) =
    N'LM-LEAVE-HR';

DECLARE @InitialVacationDays decimal(9, 2) =
    12;

DECLARE @EmployeeRoleId int;
DECLARE @HrRoleId int;
DECLARE @VacationLeaveTypeId int;
DECLARE @VacationLeavePolicyId int;
DECLARE @EmployeeUserId int;
DECLARE @HrUserId int;
DECLARE @DepartmentId int;
DECLARE @EmployeeId int;
DECLARE @HrEmployeeId int;

BEGIN TRANSACTION;

SELECT
    @EmployeeRoleId =
        R.RoleId
FROM Security.Roles AS R
WHERE R.RoleCode = N'Employee';

SELECT
    @HrRoleId =
        R.RoleId
FROM Security.Roles AS R
WHERE R.RoleCode = N'HumanResourcesAdministrator';

IF @EmployeeRoleId IS NULL
   OR @HrRoleId IS NULL
BEGIN
    THROW 59001,
        N'Required roles were not found. Publish Security.Roles first.',
        1;
END;

SELECT
    @VacationLeaveTypeId =
        LT.LeaveTypeId
FROM LeaveManagement.LeaveTypes AS LT
WHERE LT.LeaveTypeCode = N'Vacation'
    AND LT.IsActive = 1;

IF @VacationLeaveTypeId IS NULL
BEGIN
    THROW 59002,
        N'The active Vacation leave type was not found.',
        1;
END;

SELECT TOP (1)
    @VacationLeavePolicyId =
        LP.LeavePolicyId
FROM LeaveManagement.LeavePolicies AS LP
WHERE LP.LeaveTypeId = @VacationLeaveTypeId
    AND LP.IsActive = 1
ORDER BY
    LP.LeavePolicyId DESC;

IF @VacationLeavePolicyId IS NULL
BEGIN
    THROW 59003,
        N'No active Vacation leave policy was found.',
        1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM Security.Users AS U
    WHERE U.EmailAddress = @EmployeeEmailAddress
)
BEGIN
    INSERT INTO Security.Users
    (
        RoleId,
        EmailAddress,
        PasswordHash,
        IsEmailConfirmed,
        IsActive,
        RequiresPasswordChange,
        TemporaryPasswordExpiresAtUtc,
        PasswordChangedAtUtc
    )
    VALUES
    (
        @EmployeeRoleId,
        @EmployeeEmailAddress,
        @EmployeePasswordHash,
        1,
        1,
        0,
        NULL,
        SYSUTCDATETIME()
    );
END
ELSE
BEGIN
    UPDATE Security.Users
    SET
        RoleId = @EmployeeRoleId,
        PasswordHash = @EmployeePasswordHash,
        TokenVersion = TokenVersion + 1,
        IsEmailConfirmed = 1,
        IsActive = 1,
        RequiresPasswordChange = 0,
        TemporaryPasswordExpiresAtUtc = NULL,
        PasswordChangedAtUtc = SYSUTCDATETIME(),
        FailedLoginAttempts = 0,
        LockoutEndAtUtc = NULL,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE EmailAddress = @EmployeeEmailAddress;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM Security.Users AS U
    WHERE U.EmailAddress = @HrEmailAddress
)
BEGIN
    INSERT INTO Security.Users
    (
        RoleId,
        EmailAddress,
        PasswordHash,
        IsEmailConfirmed,
        IsActive,
        RequiresPasswordChange,
        TemporaryPasswordExpiresAtUtc,
        PasswordChangedAtUtc
    )
    VALUES
    (
        @HrRoleId,
        @HrEmailAddress,
        @HrPasswordHash,
        1,
        1,
        0,
        NULL,
        SYSUTCDATETIME()
    );
END
ELSE
BEGIN
    UPDATE Security.Users
    SET
        RoleId = @HrRoleId,
        PasswordHash = @HrPasswordHash,
        TokenVersion = TokenVersion + 1,
        IsEmailConfirmed = 1,
        IsActive = 1,
        RequiresPasswordChange = 0,
        TemporaryPasswordExpiresAtUtc = NULL,
        PasswordChangedAtUtc = SYSUTCDATETIME(),
        FailedLoginAttempts = 0,
        LockoutEndAtUtc = NULL,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE EmailAddress = @HrEmailAddress;
END;

SELECT
    @EmployeeUserId =
        U.UserId
FROM Security.Users AS U
WHERE U.EmailAddress = @EmployeeEmailAddress;

SELECT
    @HrUserId =
        U.UserId
FROM Security.Users AS U
WHERE U.EmailAddress = @HrEmailAddress;

IF NOT EXISTS
(
    SELECT 1
    FROM HumanResources.Departments AS D
    WHERE D.DepartmentCode = @DepartmentCode
)
BEGIN
    INSERT INTO HumanResources.Departments
    (
        DepartmentCode,
        Name,
        Description,
        IsActive,
        CreatedByUserId
    )
    VALUES
    (
        @DepartmentCode,
        @DepartmentName,
        N'Departamento usado para pruebas manuales de vacaciones.',
        1,
        @HrUserId
    );
END
ELSE
BEGIN
    UPDATE HumanResources.Departments
    SET
        Name = @DepartmentName,
        Description =
            N'Departamento usado para pruebas manuales de vacaciones.',
        IsActive = 1,
        UpdatedAtUtc = SYSUTCDATETIME(),
        UpdatedByUserId = @HrUserId
    WHERE DepartmentCode = @DepartmentCode;
END;

SELECT
    @DepartmentId =
        D.DepartmentId
FROM HumanResources.Departments AS D
WHERE D.DepartmentCode = @DepartmentCode;

IF EXISTS
(
    SELECT 1
    FROM HumanResources.Employees AS E
    WHERE E.IdentificationNumber = @EmployeeIdentificationNumber
        AND E.UserId IS NOT NULL
        AND E.UserId <> @EmployeeUserId
)
BEGIN
    THROW 59004,
        N'The employee test identification number is assigned to another user.',
        1;
END;

IF
(
    SELECT COUNT(1)
    FROM HumanResources.Employees AS E
    WHERE E.UserId = @EmployeeUserId
        OR E.IdentificationNumber = @EmployeeIdentificationNumber
) > 1
BEGIN
    THROW 59006,
        N'The employee test user and identification number are assigned to different employee records.',
        1;
END;

IF EXISTS
(
    SELECT 1
    FROM HumanResources.Employees AS E
    WHERE E.IdentificationNumber = @HrIdentificationNumber
        AND E.UserId IS NOT NULL
        AND E.UserId <> @HrUserId
)
BEGIN
    THROW 59005,
        N'The HR test identification number is assigned to another user.',
        1;
END;

IF
(
    SELECT COUNT(1)
    FROM HumanResources.Employees AS E
    WHERE E.UserId = @HrUserId
        OR E.IdentificationNumber = @HrIdentificationNumber
) > 1
BEGIN
    THROW 59007,
        N'The HR test user and identification number are assigned to different employee records.',
        1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM HumanResources.Employees AS E
    WHERE E.UserId = @EmployeeUserId
        OR E.IdentificationNumber = @EmployeeIdentificationNumber
)
BEGIN
    INSERT INTO HumanResources.Employees
    (
        UserId,
        DepartmentId,
        IdentificationNumber,
        FirstName,
        LastName,
        HireDate,
        JobTitle,
        BaseSalary,
        IsActive,
        CreatedByUserId
    )
    VALUES
    (
        @EmployeeUserId,
        @DepartmentId,
        @EmployeeIdentificationNumber,
        N'Empleado',
        N'Vacaciones',
        CONVERT(date, SYSUTCDATETIME()),
        N'Colaborador de pruebas',
        0,
        1,
        @HrUserId
    );
END
ELSE
BEGIN
    UPDATE HumanResources.Employees
    SET
        DepartmentId = @DepartmentId,
        IdentificationNumber = @EmployeeIdentificationNumber,
        FirstName = N'Empleado',
        LastName = N'Vacaciones',
        HireDate = CONVERT(date, SYSUTCDATETIME()),
        TerminationDate = NULL,
        JobTitle = N'Colaborador de pruebas',
        BaseSalary = 0,
        IsActive = 1,
        UpdatedAtUtc = SYSUTCDATETIME(),
        UpdatedByUserId = @HrUserId
    WHERE UserId = @EmployeeUserId
        OR IdentificationNumber = @EmployeeIdentificationNumber;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM HumanResources.Employees AS E
    WHERE E.UserId = @HrUserId
        OR E.IdentificationNumber = @HrIdentificationNumber
)
BEGIN
    INSERT INTO HumanResources.Employees
    (
        UserId,
        DepartmentId,
        IdentificationNumber,
        FirstName,
        LastName,
        HireDate,
        JobTitle,
        BaseSalary,
        IsActive,
        CreatedByUserId
    )
    VALUES
    (
        @HrUserId,
        @DepartmentId,
        @HrIdentificationNumber,
        N'RRHH',
        N'Vacaciones',
        CONVERT(date, SYSUTCDATETIME()),
        N'Administrador de recursos humanos',
        0,
        1,
        @HrUserId
    );
END
ELSE
BEGIN
    UPDATE HumanResources.Employees
    SET
        DepartmentId = @DepartmentId,
        IdentificationNumber = @HrIdentificationNumber,
        FirstName = N'RRHH',
        LastName = N'Vacaciones',
        HireDate = CONVERT(date, SYSUTCDATETIME()),
        TerminationDate = NULL,
        JobTitle = N'Administrador de recursos humanos',
        BaseSalary = 0,
        IsActive = 1,
        UpdatedAtUtc = SYSUTCDATETIME(),
        UpdatedByUserId = @HrUserId
    WHERE UserId = @HrUserId
        OR IdentificationNumber = @HrIdentificationNumber;
END;

SELECT
    @EmployeeId =
        E.EmployeeId
FROM HumanResources.Employees AS E
WHERE E.UserId = @EmployeeUserId;

SELECT
    @HrEmployeeId =
        E.EmployeeId
FROM HumanResources.Employees AS E
WHERE E.UserId = @HrUserId;

DECLARE @TestEmployeeIds TABLE
(
    EmployeeId int NOT NULL PRIMARY KEY
);

INSERT INTO @TestEmployeeIds
(
    EmployeeId
)
VALUES
(
    @EmployeeId
),
(
    @HrEmployeeId
);

DELETE LBT
FROM LeaveManagement.LeaveBalanceTransactions AS LBT
WHERE EXISTS
(
    SELECT 1
    FROM LeaveManagement.EmployeeLeaveBalances AS ELB
    INNER JOIN @TestEmployeeIds AS E
        ON E.EmployeeId = ELB.EmployeeId
    WHERE ELB.EmployeeLeaveBalanceId =
        LBT.EmployeeLeaveBalanceId
)
OR EXISTS
(
    SELECT 1
    FROM LeaveManagement.LeaveRequests AS LR
    INNER JOIN @TestEmployeeIds AS E
        ON E.EmployeeId = LR.EmployeeId
    WHERE LR.LeaveRequestId = LBT.LeaveRequestId
);

DELETE LRSH
FROM LeaveManagement.LeaveRequestStatusHistory AS LRSH
INNER JOIN LeaveManagement.LeaveRequests AS LR
    ON LR.LeaveRequestId = LRSH.LeaveRequestId
INNER JOIN @TestEmployeeIds AS E
    ON E.EmployeeId = LR.EmployeeId;

DELETE LR
FROM LeaveManagement.LeaveRequests AS LR
INNER JOIN @TestEmployeeIds AS E
    ON E.EmployeeId = LR.EmployeeId;

DELETE ELB
FROM LeaveManagement.EmployeeLeaveBalances AS ELB
INNER JOIN @TestEmployeeIds AS E
    ON E.EmployeeId = ELB.EmployeeId;

INSERT INTO LeaveManagement.EmployeeLeaveBalances
(
    EmployeeId,
    LeaveTypeId,
    LeavePolicyId,
    AccruedDays,
    AdjustedDays,
    PendingDays,
    UsedDays,
    CreatedByUserId
)
SELECT
    E.EmployeeId,
    @VacationLeaveTypeId,
    @VacationLeavePolicyId,
    0,
    @InitialVacationDays,
    0,
    0,
    @HrUserId
FROM @TestEmployeeIds AS E;

COMMIT TRANSACTION;

SELECT
    U.EmailAddress,
    R.RoleCode,
    E.EmployeeId,
    E.IdentificationNumber,
    ELB.AvailableDays
FROM Security.Users AS U
INNER JOIN Security.Roles AS R
    ON R.RoleId = U.RoleId
INNER JOIN HumanResources.Employees AS E
    ON E.UserId = U.UserId
INNER JOIN LeaveManagement.EmployeeLeaveBalances AS ELB
    ON ELB.EmployeeId = E.EmployeeId
INNER JOIN LeaveManagement.LeaveTypes AS LT
    ON LT.LeaveTypeId = ELB.LeaveTypeId
WHERE U.EmailAddress IN
(
    @EmployeeEmailAddress,
    @HrEmailAddress
)
AND LT.LeaveTypeCode = N'Vacation'
ORDER BY
    R.RoleCode,
    U.EmailAddress;
