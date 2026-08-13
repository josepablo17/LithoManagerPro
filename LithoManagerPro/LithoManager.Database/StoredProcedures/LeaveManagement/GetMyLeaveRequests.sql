CREATE PROCEDURE [LeaveManagement].[GetMyLeaveRequests]
    @ActorUserId int,
    @LeaveRequestStatusCode nvarchar(4000) = NULL,
    @StartDateFrom date = NULL,
    @StartDateTo date = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedLeaveRequestStatusCode nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@LeaveRequestStatusCode)),
            N''
        );

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 53101,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @NormalizedLeaveRequestStatusCode IS NOT NULL
       AND LEN(@NormalizedLeaveRequestStatusCode) > 30
    BEGIN
        THROW 53102,
            N'LeaveRequestStatusCode cannot exceed 30 characters.',
            1;
    END;

    IF @StartDateFrom IS NOT NULL
       AND @StartDateTo IS NOT NULL
       AND @StartDateTo < @StartDateFrom
    BEGIN
        THROW 53103,
            N'StartDateTo cannot be earlier than StartDateFrom.',
            1;
    END;

    DECLARE @ActorEmployeeId int;
    DECLARE @IsActorUserActive bit;
    DECLARE @IsActorRoleActive bit;
    DECLARE @IsActorEmployeeActive bit;
    DECLARE @IsActorDepartmentActive bit;

    SELECT
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

    IF @IsActorUserActive IS NULL
    BEGIN
        THROW 53104,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0
    BEGIN
        THROW 53105,
            N'The actor user account is inactive.',
            1;
    END;

    IF @IsActorRoleActive = 0
    BEGIN
        THROW 53106,
            N'The actor role is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NULL
    BEGIN
        THROW 53107,
            N'The actor user is not linked to an employee.',
            1;
    END;

    IF @IsActorEmployeeActive <> 1
    BEGIN
        THROW 53108,
            N'The actor employee record is inactive.',
            1;
    END;

    IF @IsActorDepartmentActive <> 1
    BEGIN
        THROW 53109,
            N'The actor department is inactive.',
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
    WHERE LR.[EmployeeId] = @ActorEmployeeId
        AND
        (
            @NormalizedLeaveRequestStatusCode IS NULL
            OR LR.[LeaveRequestStatusCode] = @NormalizedLeaveRequestStatusCode
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
    ORDER BY
        LR.[StartDate] DESC,
        LR.[LeaveRequestId] DESC;
END;
GO
