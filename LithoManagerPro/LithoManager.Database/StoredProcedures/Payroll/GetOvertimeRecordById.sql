CREATE PROCEDURE [Payroll].[GetOvertimeRecordById]
    @OvertimeRecordId int,
    @ActorUserId int
AS
BEGIN
    SET NOCOUNT ON;

    IF @OvertimeRecordId IS NULL OR @OvertimeRecordId <= 0
    BEGIN
        THROW 57201,
            N'OvertimeRecordId must be greater than zero.',
            1;
    END;

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 57202,
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
        THROW 57203,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
    BEGIN
        THROW 57204,
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
        THROW 57205,
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
        THROW 57206,
            N'The actor role is not allowed to query overtime records.',
            1;
    END;

    SELECT
        OTR.[OvertimeRecordId],
        OTR.[EmployeeId],
        E.[IdentificationType],
        E.[IdentificationNumber],
        E.[FirstName],
        E.[LastName],
        OTR.[AttendanceRecordId],
        OTR.[OvertimeRuleId],
        ORU.[OvertimeRuleCode],
        ORU.[Name] AS [OvertimeRuleName],
        ORU.[HourMultiplier],
        OTR.[OvertimeDate],
        OTR.[Hours],
        OTR.[ApprovalStatus],
        OTR.[ApprovedAtUtc],
        OTR.[ApprovedByUserId],
        OTR.[RejectedAtUtc],
        OTR.[RejectedByUserId],
        OTR.[RejectionReason],
        OTR.[Notes],
        OTR.[CreatedAtUtc],
        OTR.[CreatedByUserId],
        OTR.[UpdatedAtUtc],
        OTR.[UpdatedByUserId],
        OTR.[RowVersion]
    FROM [Payroll].[OvertimeRecords] AS OTR
    INNER JOIN [HumanResources].[Employees] AS E
        ON E.[EmployeeId] = OTR.[EmployeeId]
    INNER JOIN [Payroll].[OvertimeRules] AS ORU
        ON ORU.[OvertimeRuleId] = OTR.[OvertimeRuleId]
    WHERE OTR.[OvertimeRecordId] = @OvertimeRecordId;
END;
GO
