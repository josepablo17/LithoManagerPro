CREATE PROCEDURE [LeaveManagement].[GetLeaveRequestById]
    @LeaveRequestId int,
    @ActorUserId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @LeaveRequestId IS NULL
       OR @LeaveRequestId <= 0
    BEGIN
        RETURN;
    END;

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 53701,
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
        THROW 53702,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0
    BEGIN
        THROW 53703,
            N'The actor user account is inactive.',
            1;
    END;

    IF @IsActorRoleActive = 0
    BEGIN
        THROW 53704,
            N'The actor role is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND @IsActorEmployeeActive <> 1
    BEGIN
        THROW 53705,
            N'The actor employee record is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND @IsActorDepartmentActive <> 1
    BEGIN
        THROW 53706,
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
        RU.[EmailAddress] AS [RespondedByEmailAddress],
        LR.[CancelledAtUtc],
        LR.[CancelledByUserId],
        CU.[EmailAddress] AS [CancelledByEmailAddress],
        LR.[CreatedAtUtc],
        LR.[CreatedByUserId],
        CRU.[EmailAddress] AS [CreatedByEmailAddress],
        LR.[UpdatedAtUtc],
        LR.[UpdatedByUserId],
        UU.[EmailAddress] AS [UpdatedByEmailAddress],
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
    LEFT JOIN [Security].[Users] AS RU
        ON RU.[UserId] = LR.[RespondedByUserId]
    LEFT JOIN [Security].[Users] AS CU
        ON CU.[UserId] = LR.[CancelledByUserId]
    LEFT JOIN [Security].[Users] AS CRU
        ON CRU.[UserId] = LR.[CreatedByUserId]
    LEFT JOIN [Security].[Users] AS UU
        ON UU.[UserId] = LR.[UpdatedByUserId]
    WHERE LR.[LeaveRequestId] = @LeaveRequestId
        AND
        (
            LR.[EmployeeId] = @ActorEmployeeId
            OR @ActorRoleCode IN
            (
                N'SuperAdministrator',
                N'HumanResourcesAdministrator',
                N'HumanResourcesStaff'
            )
        );
END;
GO
