CREATE PROCEDURE [LeaveManagement].[GetLeaveRequests]
    @ActorUserId int,
    @LeaveRequestStatusCode nvarchar(4000) = N'Pending',
    @EmployeeId int = NULL,
    @DepartmentId int = NULL,
    @StartDateFrom date = NULL,
    @StartDateTo date = NULL,
    @SearchTerm nvarchar(4000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedLeaveRequestStatusCode nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@LeaveRequestStatusCode)),
            N''
        );

    DECLARE @NormalizedSearchTerm nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@SearchTerm)),
            N''
        );

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 53201,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @NormalizedLeaveRequestStatusCode IS NOT NULL
       AND LEN(@NormalizedLeaveRequestStatusCode) > 30
    BEGIN
        THROW 53202,
            N'LeaveRequestStatusCode cannot exceed 30 characters.',
            1;
    END;

    IF @EmployeeId IS NOT NULL
       AND @EmployeeId <= 0
    BEGIN
        THROW 53203,
            N'EmployeeId must be greater than zero when provided.',
            1;
    END;

    IF @DepartmentId IS NOT NULL
       AND @DepartmentId <= 0
    BEGIN
        THROW 53204,
            N'DepartmentId must be greater than zero when provided.',
            1;
    END;

    IF @StartDateFrom IS NOT NULL
       AND @StartDateTo IS NOT NULL
       AND @StartDateTo < @StartDateFrom
    BEGIN
        THROW 53205,
            N'StartDateTo cannot be earlier than StartDateFrom.',
            1;
    END;

    IF @NormalizedSearchTerm IS NOT NULL
       AND LEN(@NormalizedSearchTerm) > 150
    BEGIN
        THROW 53206,
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
        @ActorRoleCode =
            R.[RoleCode],
        @IsActorUserActive =
            U.[IsActive],
        @IsActorRoleActive =
            R.[IsActive],
        @ActorEmployeeId =
            E.[EmployeeId],
        @IsActorEmployeeActive =
            E.[IsActive],
        @IsActorDepartmentActive =
            D.[IsActive]
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
        THROW 53207,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0
    BEGIN
        THROW 53208,
            N'The actor user account is inactive.',
            1;
    END;

    IF @IsActorRoleActive = 0
    BEGIN
        THROW 53209,
            N'The actor role is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND @IsActorEmployeeActive <> 1
    BEGIN
        THROW 53210,
            N'The actor employee record is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND @IsActorDepartmentActive <> 1
    BEGIN
        THROW 53211,
            N'The actor department is inactive.',
            1;
    END;

    IF @ActorRoleCode NOT IN
    (
        N'SuperAdministrator',
        N'HumanResourcesAdministrator',
        N'HumanResourcesStaff'
    )
    BEGIN
        THROW 53212,
            N'The actor role is not allowed to list leave requests.',
            1;
    END;

    SELECT
        LR.[LeaveRequestId],
        LR.[EmployeeId],
        E.[IdentificationNumber],
        E.[FirstName],
        E.[LastName],
        D.[DepartmentId],
        D.[DepartmentCode],
        D.[Name] AS [DepartmentName],
        LR.[LeaveTypeId],
        LT.[LeaveTypeCode],
        LT.[Name] AS [LeaveTypeName],
        LR.[LeaveRequestStatusCode],
        LRS.[Name] AS [LeaveRequestStatusName],
        LR.[StartDate],
        LR.[EndDate],
        LR.[RequestedDays],
        LR.[RespondedAtUtc],
        LR.[RespondedByUserId],
        LR.[CancelledAtUtc],
        LR.[CancelledByUserId],
        LR.[CreatedAtUtc],
        LR.[CreatedByUserId],
        LR.[UpdatedAtUtc],
        LR.[UpdatedByUserId],
        LR.[RowVersion]
    FROM [LeaveManagement].[LeaveRequests] AS LR
    INNER JOIN [HumanResources].[Employees] AS E
        ON E.[EmployeeId] = LR.[EmployeeId]
    INNER JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]
    INNER JOIN [LeaveManagement].[LeaveTypes] AS LT
        ON LT.[LeaveTypeId] = LR.[LeaveTypeId]
    INNER JOIN [LeaveManagement].[LeaveRequestStatuses] AS LRS
        ON LRS.[LeaveRequestStatusCode] = LR.[LeaveRequestStatusCode]
    WHERE
        (
            @NormalizedLeaveRequestStatusCode IS NULL
            OR LR.[LeaveRequestStatusCode] = @NormalizedLeaveRequestStatusCode
        )
        AND
        (
            @EmployeeId IS NULL
            OR LR.[EmployeeId] = @EmployeeId
        )
        AND
        (
            @DepartmentId IS NULL
            OR E.[DepartmentId] = @DepartmentId
        )
        AND
        (
            @StartDateFrom IS NULL
            OR LR.[StartDate] >= @StartDateFrom
        )
        AND
        (
            @StartDateTo IS NULL
            OR LR.[StartDate] <= @StartDateTo
        )
        AND
        (
            @NormalizedSearchTerm IS NULL
            OR E.[IdentificationNumber] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR E.[FirstName] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR E.[LastName] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR D.[DepartmentCode] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
            OR D.[Name] LIKE
                N'%' + @NormalizedSearchTerm + N'%'
        )
    ORDER BY
        CASE
            WHEN LR.[LeaveRequestStatusCode] = N'Pending'
                THEN 0
            ELSE 1
        END,
        LR.[StartDate] DESC,
        LR.[LeaveRequestId] DESC;
END;
GO
