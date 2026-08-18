CREATE PROCEDURE [Payroll].[GetAttendanceRecords]
    @ActorUserId int,
    @EmployeeId int = NULL,
    @DepartmentId int = NULL,
    @AttendanceStatus nvarchar(4000) = NULL,
    @IsApproved bit = NULL,
    @DateFrom date = NULL,
    @DateTo date = NULL,
    @SearchTerm nvarchar(4000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedAttendanceStatus nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@AttendanceStatus)), N'');

    DECLARE @NormalizedSearchTerm nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 56901,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @EmployeeId IS NOT NULL AND @EmployeeId <= 0
    BEGIN
        THROW 56902,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @DepartmentId IS NOT NULL AND @DepartmentId <= 0
    BEGIN
        THROW 56903,
            N'DepartmentId must be greater than zero.',
            1;
    END;

    IF @NormalizedAttendanceStatus IS NOT NULL
       AND @NormalizedAttendanceStatus NOT IN
       (
           N'Present',
           N'Partial',
           N'Absent',
           N'Holiday',
           N'Leave',
           N'Disability'
       )
    BEGIN
        THROW 56904,
            N'AttendanceStatus is invalid.',
            1;
    END;

    IF @DateFrom IS NOT NULL
       AND @DateTo IS NOT NULL
       AND @DateTo < @DateFrom
    BEGIN
        THROW 56905,
            N'DateTo cannot be earlier than DateFrom.',
            1;
    END;

    IF @NormalizedSearchTerm IS NOT NULL
       AND LEN(@NormalizedSearchTerm) > 150
    BEGIN
        THROW 56906,
            N'SearchTerm cannot exceed 150 characters.',
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
        THROW 56907,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
    BEGIN
        THROW 56908,
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
        THROW 56909,
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
        THROW 56910,
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
    INNER JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]
    INNER JOIN [Payroll].[WorkShiftTypes] AS WST
        ON WST.[WorkShiftTypeId] = AR.[WorkShiftTypeId]
    WHERE
        (@EmployeeId IS NULL OR AR.[EmployeeId] = @EmployeeId)
        AND
        (@DepartmentId IS NULL OR D.[DepartmentId] = @DepartmentId)
        AND
        (
            @NormalizedAttendanceStatus IS NULL
            OR AR.[AttendanceStatus] = @NormalizedAttendanceStatus
        )
        AND
        (@IsApproved IS NULL OR AR.[IsApproved] = @IsApproved)
        AND
        (@DateFrom IS NULL OR AR.[AttendanceDate] >= @DateFrom)
        AND
        (@DateTo IS NULL OR AR.[AttendanceDate] <= @DateTo)
        AND
        (
            @NormalizedSearchTerm IS NULL
            OR E.[IdentificationNumber] LIKE N'%' + @NormalizedSearchTerm + N'%'
            OR E.[FirstName] LIKE N'%' + @NormalizedSearchTerm + N'%'
            OR E.[LastName] LIKE N'%' + @NormalizedSearchTerm + N'%'
        )
    ORDER BY
        AR.[AttendanceDate] DESC,
        E.[LastName],
        E.[FirstName],
        AR.[AttendanceRecordId] DESC;
END;
GO
