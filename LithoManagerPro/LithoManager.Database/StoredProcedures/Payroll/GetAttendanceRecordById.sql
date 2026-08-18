CREATE PROCEDURE [Payroll].[GetAttendanceRecordById]
    @AttendanceRecordId int,
    @ActorUserId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @AttendanceRecordId IS NULL OR @AttendanceRecordId <= 0
    BEGIN
        THROW 57001,
            N'AttendanceRecordId must be greater than zero.',
            1;
    END;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 57002,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    DECLARE @ActorRoleCode nvarchar(50);
    DECLARE @IsActorUserActive bit;
    DECLARE @IsActorRoleActive bit;
    DECLARE @ActorEmployeeId int;
    DECLARE @IsActorEmployeeActive bit;
    DECLARE @IsActorDepartmentActive bit;

    SELECT
        @ActorRoleCode = R.[RoleCode],
        @IsActorUserActive = U.[IsActive],
        @IsActorRoleActive = R.[IsActive],
        @ActorEmployeeId = E.[EmployeeId],
        @IsActorEmployeeActive = E.[IsActive],
        @IsActorDepartmentActive = D.[IsActive]
    FROM [Security].[Users] AS U
    INNER JOIN [Security].[Roles] AS R
        ON R.[RoleId] = U.[RoleId]
    LEFT JOIN [HumanResources].[Employees] AS E
        ON E.[UserId] = U.[UserId]
    LEFT JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]
    WHERE U.[UserId] = @ActorUserId;

    IF @ActorRoleCode IS NULL
    BEGIN
        THROW 57003,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
    BEGIN
        THROW 57004,
            N'The actor user or role is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND
       (
           @IsActorEmployeeActive <> 1
           OR @IsActorDepartmentActive <> 1
       )
    BEGIN
        THROW 57005,
            N'The actor employee or department is inactive.',
            1;
    END;

    IF @ActorRoleCode NOT IN
    (
        N'SuperAdministrator',
        N'HumanResourcesAdministrator',
        N'HumanResourcesStaff'
    )
    BEGIN
        THROW 57006,
            N'The actor role is not allowed to query attendance records.',
            1;
    END;

    SELECT
        AR.[AttendanceRecordId],
        AR.[EmployeeId],
        E.[IdentificationType],
        E.[IdentificationNumber],
        E.[FirstName],
        E.[LastName],
        AR.[WorkShiftTypeId],
        WST.[WorkShiftTypeCode],
        WST.[Name] AS [WorkShiftTypeName],
        AR.[AttendanceDate],
        AR.[AttendanceStatus],
        AR.[ExpectedHours],
        AR.[WorkedHours],
        AR.[PaidHours],
        AR.[UnpaidHours],
        AR.[IsPaidHoliday],
        AR.[IsApproved],
        AR.[ApprovedAtUtc],
        AR.[ApprovedByUserId],
        AR.[Notes],
        AR.[CreatedAtUtc],
        AR.[CreatedByUserId],
        AR.[UpdatedAtUtc],
        AR.[UpdatedByUserId],
        AR.[RowVersion]
    FROM [Payroll].[AttendanceRecords] AS AR
    INNER JOIN [HumanResources].[Employees] AS E
        ON E.[EmployeeId] = AR.[EmployeeId]
    INNER JOIN [Payroll].[WorkShiftTypes] AS WST
        ON WST.[WorkShiftTypeId] = AR.[WorkShiftTypeId]
    WHERE AR.[AttendanceRecordId] = @AttendanceRecordId;
END;
GO
