CREATE PROCEDURE [Payroll].[GetOvertimeRecords]
    @ActorUserId int,
    @EmployeeId int = NULL,
    @DepartmentId int = NULL,
    @OvertimeRuleId int = NULL,
    @ApprovalStatus nvarchar(4000) = NULL,
    @DateFrom date = NULL,
    @DateTo date = NULL,
    @SearchTerm nvarchar(4000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormalizedApprovalStatus nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@ApprovalStatus)), N'');

    DECLARE @NormalizedSearchTerm nvarchar(4000) =
        NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
    BEGIN
        THROW 57101,
            N'The ActorUserId must be greater than zero.',
            1;
    END;

    IF @EmployeeId IS NOT NULL AND @EmployeeId <= 0
    BEGIN
        THROW 57102,
            N'EmployeeId must be greater than zero.',
            1;
    END;

    IF @DepartmentId IS NOT NULL AND @DepartmentId <= 0
    BEGIN
        THROW 57103,
            N'DepartmentId must be greater than zero.',
            1;
    END;

    IF @OvertimeRuleId IS NOT NULL AND @OvertimeRuleId <= 0
    BEGIN
        THROW 57104,
            N'OvertimeRuleId must be greater than zero.',
            1;
    END;

    IF @NormalizedApprovalStatus IS NOT NULL
       AND @NormalizedApprovalStatus NOT IN
       (
           N'Pending',
           N'Approved',
           N'Rejected',
           N'Cancelled'
       )
    BEGIN
        THROW 57105,
            N'ApprovalStatus is invalid.',
            1;
    END;

    IF @DateFrom IS NOT NULL
       AND @DateTo IS NOT NULL
       AND @DateTo < @DateFrom
    BEGIN
        THROW 57106,
            N'DateTo cannot be earlier than DateFrom.',
            1;
    END;

    IF @NormalizedSearchTerm IS NOT NULL
       AND LEN(@NormalizedSearchTerm) > 150
    BEGIN
        THROW 57107,
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
        THROW 57108,
            N'The actor user was not found.',
            1;
    END;

    IF @IsActorUserActive = 0 OR @IsActorRoleActive = 0
    BEGIN
        THROW 57109,
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
        THROW 57110,
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
        THROW 57111,
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
    INNER JOIN [HumanResources].[Departments] AS D
        ON D.[DepartmentId] = E.[DepartmentId]
    INNER JOIN [Payroll].[OvertimeRules] AS ORU
        ON ORU.[OvertimeRuleId] = OTR.[OvertimeRuleId]
    WHERE
        (@EmployeeId IS NULL OR OTR.[EmployeeId] = @EmployeeId)
        AND
        (@DepartmentId IS NULL OR D.[DepartmentId] = @DepartmentId)
        AND
        (@OvertimeRuleId IS NULL OR OTR.[OvertimeRuleId] = @OvertimeRuleId)
        AND
        (
            @NormalizedApprovalStatus IS NULL
            OR OTR.[ApprovalStatus] = @NormalizedApprovalStatus
        )
        AND
        (@DateFrom IS NULL OR OTR.[OvertimeDate] >= @DateFrom)
        AND
        (@DateTo IS NULL OR OTR.[OvertimeDate] <= @DateTo)
        AND
        (
            @NormalizedSearchTerm IS NULL
            OR E.[IdentificationNumber] LIKE N'%' + @NormalizedSearchTerm + N'%'
            OR E.[FirstName] LIKE N'%' + @NormalizedSearchTerm + N'%'
            OR E.[LastName] LIKE N'%' + @NormalizedSearchTerm + N'%'
        )
    ORDER BY
        OTR.[OvertimeDate] DESC,
        E.[LastName],
        E.[FirstName],
        OTR.[OvertimeRecordId] DESC;
END;
GO
