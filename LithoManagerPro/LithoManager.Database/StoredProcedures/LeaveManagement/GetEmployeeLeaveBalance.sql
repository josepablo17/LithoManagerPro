CREATE PROCEDURE [LeaveManagement].[GetEmployeeLeaveBalance]
    @EmployeeId int = NULL,
    @LeaveTypeCode nvarchar(4000) = N'Vacation',
    @ActorUserId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedLeaveTypeCode nvarchar(4000) =
        NULLIF(
            LTRIM(RTRIM(@LeaveTypeCode)),
            N''
        );

    IF @ActorUserId IS NULL
       OR @ActorUserId <= 0
    BEGIN
        THROW 53001,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @EmployeeId IS NOT NULL
       AND @EmployeeId <= 0
    BEGIN
        THROW 53002,
            N'EmployeeId must be greater than zero when provided.',
            1;
    END;

    IF @NormalizedLeaveTypeCode IS NULL
    BEGIN
        THROW 53003,
            N'LeaveTypeCode is required.',
            1;
    END;

    IF LEN(@NormalizedLeaveTypeCode) > 50
    BEGIN
        THROW 53004,
            N'LeaveTypeCode cannot exceed 50 characters.',
            1;
    END;

    DECLARE @ActorRoleCode nvarchar(50);
    DECLARE @IsActorUserActive bit;
    DECLARE @IsActorRoleActive bit;
    DECLARE @ActorEmployeeId int;
    DECLARE @IsActorEmployeeActive bit;
    DECLARE @IsActorDepartmentActive bit;
    DECLARE @ResolvedEmployeeId int;

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
        THROW 53005,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0
    BEGIN
        THROW 53006,
            N'The actor user account is inactive.',
            1;
    END;

    IF @IsActorRoleActive = 0
    BEGIN
        THROW 53007,
            N'The actor role is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND @IsActorEmployeeActive <> 1
    BEGIN
        THROW 53008,
            N'The actor employee record is inactive.',
            1;
    END;

    IF @ActorEmployeeId IS NOT NULL
       AND @IsActorDepartmentActive <> 1
    BEGIN
        THROW 53009,
            N'The actor department is inactive.',
            1;
    END;

    SET @ResolvedEmployeeId =
        COALESCE(
            @EmployeeId,
            @ActorEmployeeId
        );

    IF @ResolvedEmployeeId IS NULL
    BEGIN
        THROW 53010,
            N'The actor user is not linked to an employee.',
            1;
    END;

    IF @EmployeeId IS NOT NULL
       AND @EmployeeId <> @ActorEmployeeId
       AND @ActorRoleCode NOT IN
       (
           N'SuperAdministrator',
           N'HumanResourcesAdministrator',
           N'HumanResourcesStaff'
       )
    BEGIN
        THROW 53011,
            N'The actor role is not allowed to read another employee leave balance.',
            1;
    END;

    SELECT
        ELB.[EmployeeLeaveBalanceId],
        ELB.[EmployeeId],
        E.[IdentificationNumber],
        E.[FirstName],
        E.[LastName],
        D.[DepartmentId],
        D.[DepartmentCode],
        D.[Name] AS [DepartmentName],
        ELB.[LeaveTypeId],
        LT.[LeaveTypeCode],
        LT.[Name] AS [LeaveTypeName],
        LT.[AffectsVacationBalance],
        ELB.[LeavePolicyId],
        LP.[LeavePolicyCode],
        LP.[Name] AS [LeavePolicyName],
        LP.[EntitlementDays],
        LP.[EntitlementWeeks],
        LP.[UsesBusinessDays],
        ELB.[AccruedDays],
        ELB.[AdjustedDays],
        ELB.[PendingDays],
        ELB.[UsedDays],
        ELB.[AvailableDays],
        ELB.[CreatedAtUtc],
        ELB.[CreatedByUserId],
        ELB.[UpdatedAtUtc],
        ELB.[UpdatedByUserId],
        ELB.[RowVersion]
    FROM [LeaveManagement].[EmployeeLeaveBalances] AS ELB
    INNER JOIN [HumanResources].[Employees] AS E
        ON E.[EmployeeId] = ELB.[EmployeeId]
    INNER JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]
    INNER JOIN [LeaveManagement].[LeaveTypes] AS LT
        ON LT.[LeaveTypeId] = ELB.[LeaveTypeId]
    INNER JOIN [LeaveManagement].[LeavePolicies] AS LP
        ON LP.[LeavePolicyId] = ELB.[LeavePolicyId]
    WHERE ELB.[EmployeeId] = @ResolvedEmployeeId
        AND LT.[LeaveTypeCode] = @NormalizedLeaveTypeCode;
END;
GO
